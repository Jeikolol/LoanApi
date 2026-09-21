using Application.Exceptions;
using Application.Features.Payments.Commands;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Payments.Handlers
{
    public class ReversePaymentCommandHandler : IRequestHandler<ReversePaymentCommand, LoanPaymentResponse>
    {
        public const int MinReasonLength = 5;
        public const int MaxReasonLength = 250;

        private readonly IDbConnection _connection;
        private readonly ILogger<ReversePaymentCommandHandler> _logger;

        public ReversePaymentCommandHandler(IDbConnection connection, ILogger<ReversePaymentCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanPaymentResponse> Handle(ReversePaymentCommand request, CancellationToken cancellationToken)
        {
            string reason = (request.Reason ?? string.Empty).Trim();
            if (reason.Length is < MinReasonLength or > MaxReasonLength)
            {
                throw new ValidationException(
                    "The reversal is not valid.",
                    [$"The reason is required: between {MinReasonLength} and {MaxReasonLength} characters."]);
            }

            DateTime now = DateTime.UtcNow;

            using IDbTransaction transaction = _connection.BeginTransaction();
            try
            {
                PaymentTarget payment = await _connection.QuerySingleOrDefaultAsync<PaymentTarget>(
                    "SELECT LoanId, PrincipalAmount, InterestAmount, Status FROM [Loans].[LoanPayment] WHERE Id = @Id AND IsDeleted = 0",
                    new { request.Id }, transaction)
                    ?? throw new NotFoundException($"Payment {request.Id} not found");

                if (payment.Status != PaymentStatus.Completed)
                {
                    throw new ConflictException($"Only a completed payment can be reversed; this one is {payment.Status}.");
                }

                LoanStatus loanStatus = await _connection.QuerySingleAsync<LoanStatus>(
                    "SELECT LoanStatus FROM [Loans].[Loan] WHERE Id = @LoanId", new { payment.LoanId }, transaction);
                if (!PaymentRules.CanReversePaymentsOf(loanStatus))
                {
                    throw new ConflictException($"The payments of a loan with status {loanStatus} cannot be reversed.");
                }

                // The status guard makes it atomic: of two concurrent reversals only one finds the payment still completed.
                int reversed = await _connection.ExecuteAsync(
                    @"UPDATE [Loans].[LoanPayment]
                      SET Status = @Reversed, ReversalReason = @Reason, ReversedOn = @Now, UpdatedOn = @Now
                      WHERE Id = @Id AND IsDeleted = 0 AND Status = @Completed",
                    new { request.Id, Reason = reason, Now = now, Reversed = (int)PaymentStatus.Reversed, Completed = (int)PaymentStatus.Completed },
                    transaction);
                if (reversed != 1) throw new ConflictException("The payment was reversed in the meantime.");

                // The exact inverse of registering a payment. The guards refuse a reversal the loan's own totals cannot absorb.
                int updated = await _connection.ExecuteAsync(
                    @"UPDATE [Loans].[Loan]
                      SET PrincipalPaid = PrincipalPaid - @Principal,
                          PrincipalRemaining = PrincipalRemaining + @Principal,
                          PaidInterest = PaidInterest - @Interest,
                          RemainingInterest = CASE WHEN RemainingInterest + @Interest > TotalInterestAmount THEN TotalInterestAmount ELSE RemainingInterest + @Interest END,
                          AmortizationBalance = CASE WHEN AmortizationBalance + @Principal > PrincipalAmount THEN PrincipalAmount ELSE AmortizationBalance + @Principal END,
                          LastPaymentDate = (SELECT MAX(CreatedOn) FROM [Loans].[LoanPayment]
                                             WHERE LoanId = @LoanId AND Status = @Completed AND IsDeleted = 0),
                          UpdatedOn = @Now
                      WHERE Id = @LoanId AND IsDeleted = 0 AND PrincipalPaid >= @Principal AND PaidInterest >= @Interest",
                    new { payment.LoanId, Principal = payment.PrincipalAmount, Interest = payment.InterestAmount, Now = now, Completed = (int)PaymentStatus.Completed },
                    transaction);
                if (updated != 1) throw new ConflictException("The loan's totals do not allow reversing this payment.");

                await LoanScheduleSync.SyncAsync(_connection, payment.LoanId, DateOnly.FromDateTime(now), transaction, PaymentRules.StatusAfterReversal);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                if (ex is not CustomException)
                {
                    _logger.LogError(ex, "Error reversing payment {PaymentId}", request.Id);
                }
                throw;
            }

            _logger.LogInformation("Payment {PaymentId} reversed", request.Id);

            PaymentRow row = await _connection.QuerySingleAsync<PaymentRow>(
                @"SELECT Id, LoanId, PaymentDate, PrincipalAmount, InterestAmount, PenaltyAmount, TotalPaymentAmount,
                         PaymentMethod, ReferenceNumber, Status, CreatedOn, ReversalReason
                  FROM [Loans].[LoanPayment] WHERE Id = @Id", new { request.Id });

            return row.ToResponse();
        }

        private sealed class PaymentTarget
        {
            public Guid LoanId { get; set; }
            public decimal PrincipalAmount { get; set; }
            public decimal InterestAmount { get; set; }
            public PaymentStatus Status { get; set; }
        }
    }
}
