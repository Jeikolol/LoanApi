using Application.Exceptions;
using Application.Features.Payments;
using Application.Features.Payments.Commands;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Payments.Handlers
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, LoanPaymentResponse>
    {
        private static readonly LoanStatus[] PayableStatuses =
        [
            LoanStatus.Disbursed,
            LoanStatus.Active,
            LoanStatus.PartiallyPaid,
            LoanStatus.Defaulted,
            LoanStatus.Restructured,
        ];

        private readonly IDbConnection _connection;
        private readonly ILogger<CreatePaymentCommandHandler> _logger;

        public CreatePaymentCommandHandler(IDbConnection connection, ILogger<CreatePaymentCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanPaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            Validate(request);

            _logger.LogInformation("Creating payment for loan {LoanId}", request.LoanId);

            Guid paymentId = Guid.NewGuid();
            decimal totalPayment = request.PrincipalAmount + request.InterestAmount + request.PenaltyAmount;
            DateTime now = DateTime.UtcNow;

            using IDbTransaction transaction = _connection.BeginTransaction();
            try
            {
                LoanPaymentTarget loan = await _connection.QuerySingleOrDefaultAsync<LoanPaymentTarget>(
                    "SELECT CurrencyId, LoanStatus, PrincipalRemaining, LateFeesCharged FROM [Loans].[Loan] WHERE Id = @LoanId AND IsDeleted = 0",
                    new { request.LoanId }, transaction)
                    ?? throw new NotFoundException($"Loan {request.LoanId} not found");

                if (!PayableStatuses.Contains(loan.LoanStatus))
                {
                    throw new ConflictException($"A loan with status {loan.LoanStatus} cannot receive payments.");
                }

                if (request.PrincipalAmount > loan.PrincipalRemaining)
                {
                    throw new ValidationException(
                        "The payment is not valid.",
                        [$"PrincipalAmount cannot exceed the remaining principal ({loan.PrincipalRemaining:0.00})."]);
                }

                if (request.PenaltyAmount > 0)
                {
                    // Only late fees actually charged to the loan can be collected (reversed payments give theirs back).
                    decimal penaltiesPaid = await _connection.ExecuteScalarAsync<decimal>(
                        "SELECT COALESCE(SUM(PenaltyAmount), 0) FROM [Loans].[LoanPayment] WHERE LoanId = @LoanId AND Status = @Completed AND IsDeleted = 0",
                        new { request.LoanId, Completed = (int)PaymentStatus.Completed }, transaction);
                    decimal owed = Math.Max(0, loan.LateFeesCharged - penaltiesPaid);

                    if (request.PenaltyAmount > owed)
                    {
                        throw new ValidationException(
                            "The payment is not valid.",
                            [$"PenaltyAmount cannot exceed the late fees owed ({owed:0.00})."]);
                    }
                }

                await _connection.ExecuteAsync(
                    @"INSERT INTO [Loans].[LoanPayment]
                        (Id, LoanId, CurrencyId, PaymentDate, PrincipalAmount, InterestAmount, PenaltyAmount,
                         TotalPaymentAmount, PaymentMethod, ReferenceNumber, Status, CreatedOn, CreatedById, IsActive, IsDeleted)
                      VALUES
                        (@Id, @LoanId, @CurrencyId, @PaymentDate, @PrincipalAmount, @InterestAmount, @PenaltyAmount,
                         @TotalPaymentAmount, @PaymentMethod, @ReferenceNumber, @Status, @CreatedOn, @CreatedById, 1, 0)",
                    new
                    {
                        Id = paymentId,
                        request.LoanId,
                        loan.CurrencyId,
                        PaymentDate = DateOnly.FromDateTime(now),
                        request.PrincipalAmount,
                        request.InterestAmount,
                        request.PenaltyAmount,
                        TotalPaymentAmount = totalPayment,
                        PaymentMethod = (int)request.PaymentMethod,
                        request.ReferenceNumber,
                        Status = (int)PaymentStatus.Completed,
                        CreatedOn = now,
                        request.CreatedById,
                    },
                    transaction);

                // The PrincipalRemaining guard makes this atomic: a concurrent payment that already
                // consumed the balance leaves zero rows updated instead of driving it negative.
                int updated = await _connection.ExecuteAsync(
                    @"UPDATE [Loans].[Loan]
                      SET PrincipalPaid = PrincipalPaid + @PrincipalAmount,
                          PrincipalRemaining = PrincipalRemaining - @PrincipalAmount,
                          PaidInterest = PaidInterest + @InterestAmount,
                          RemainingInterest = CASE WHEN RemainingInterest > @InterestAmount THEN RemainingInterest - @InterestAmount ELSE 0 END,
                          AmortizationBalance = CASE WHEN AmortizationBalance > @PrincipalAmount THEN AmortizationBalance - @PrincipalAmount ELSE 0 END,
                          LastPaymentDate = @Now,
                          UpdatedOn = @Now
                      WHERE Id = @LoanId AND IsDeleted = 0 AND PrincipalRemaining >= @PrincipalAmount",
                    new
                    {
                        request.PrincipalAmount,
                        request.InterestAmount,
                        request.LoanId,
                        Now = now,
                    },
                    transaction);

                if (updated != 1)
                {
                    throw new ConflictException("The loan balance changed while processing the payment. Please refresh and try again.");
                }

                await LoanScheduleSync.SyncAsync(_connection, request.LoanId, DateOnly.FromDateTime(now), transaction, PaymentRules.StatusAfterPayment);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex is not CustomException)
                {
                    _logger.LogError(ex, "Error creating payment for loan {LoanId}", request.LoanId);
                }
                throw;
            }

            return new LoanPaymentResponse(
                paymentId, request.LoanId, now, request.PrincipalAmount, request.InterestAmount,
                request.PenaltyAmount, totalPayment, request.PaymentMethod.ToString(),
                request.ReferenceNumber, PaymentStatus.Completed, now);
        }

        private static void Validate(CreatePaymentCommand request)
        {
            List<string> errors = [];

            if (request.LoanId == Guid.Empty) errors.Add("LoanId is required.");
            if (!Enum.IsDefined(request.PaymentMethod)) errors.Add("PaymentMethod is not valid.");

            CheckAmount(errors, nameof(request.PrincipalAmount), request.PrincipalAmount);
            CheckAmount(errors, nameof(request.InterestAmount), request.InterestAmount);
            CheckAmount(errors, nameof(request.PenaltyAmount), request.PenaltyAmount);

            if (errors.Count == 0 && request.PrincipalAmount + request.InterestAmount + request.PenaltyAmount <= 0)
            {
                errors.Add("The payment total must be greater than zero.");
            }

            if (request.ReferenceNumber?.Length > 100) errors.Add("ReferenceNumber cannot exceed 100 characters.");

            if (errors.Count > 0)
            {
                throw new ValidationException("The payment is not valid.", errors);
            }
        }

        private static void CheckAmount(List<string> errors, string name, decimal amount)
        {
            if (amount < 0) errors.Add($"{name} cannot be negative.");
            else if (decimal.Round(amount, 2) != amount) errors.Add($"{name} cannot have more than two decimals.");
        }

        private sealed class LoanPaymentTarget
        {
            public Guid CurrencyId { get; set; }
            public LoanStatus LoanStatus { get; set; }
            public decimal PrincipalRemaining { get; set; }
            public decimal LateFeesCharged { get; set; }
        }
    }
}
