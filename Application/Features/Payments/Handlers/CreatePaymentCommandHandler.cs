using Application.Features.Payments.Commands;
using Application.Models.Responses;
using Domain.Enums;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Payments.Handlers
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, LoanPaymentResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CreatePaymentCommandHandler> _logger;

        public CreatePaymentCommandHandler(IDbConnection connection, ILogger<CreatePaymentCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanPaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating payment for loan {LoanId}", request.LoanId);

            var paymentId = Guid.NewGuid();
            var totalPayment = request.PrincipalAmount + request.InterestAmount + request.PenaltyAmount;
            var now = DateTime.UtcNow;

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    const string insertPaymentSql = @"
                        INSERT INTO [Loans].[LoanPayment]
                        (Id, LoanId, CurrencyId, PaymentDate, PrincipalAmount, InterestAmount, PenaltyAmount,
                         TotalPaymentAmount, PaymentMethod, ReferenceNumber, Status, CreatedOn, IsActive, IsDeleted)
                        VALUES
                        (@Id, @LoanId, @CurrencyId, @PaymentDate, @PrincipalAmount, @InterestAmount, @PenaltyAmount,
                         @TotalPaymentAmount, @PaymentMethod, @ReferenceNumber, @Status, @CreatedOn, 1, 0)";

                    await _connection.ExecuteAsync(insertPaymentSql, new
                    {
                        Id = paymentId,
                        request.LoanId,
                        request.CurrencyId,
                        PaymentDate = DateOnly.FromDateTime(now),
                        request.PrincipalAmount,
                        request.InterestAmount,
                        request.PenaltyAmount,
                        TotalPaymentAmount = totalPayment,
                        PaymentMethod = (int)request.PaymentMethod,
                        request.ReferenceNumber,
                        Status = (int)PaymentStatus.Completed,
                        CreatedOn = now
                    }, transaction);

                    const string updateLoanSql = @"
                        UPDATE [Loans].[Loan]
                        SET PrincipalPaid = PrincipalPaid + @PrincipalAmount,
                            PrincipalRemaining = PrincipalRemaining - @PrincipalAmount,
                            PaidInterest = PaidInterest + @InterestAmount,
                            RemainingInterest = RemainingInterest - @InterestAmount,
                            AmortizationBalance = AmortizationBalance - @PrincipalAmount,
                            LastPaymentDate = @Now,
                            UpdatedOn = @Now,
                            LoanStatus = CASE WHEN (PrincipalRemaining - @PrincipalAmount) <= 0 THEN 5 ELSE LoanStatus END
                        WHERE Id = @LoanId AND IsDeleted = 0";

                    await _connection.ExecuteAsync(updateLoanSql, new
                    {
                        request.PrincipalAmount,
                        request.InterestAmount,
                        request.LoanId,
                        Now = now
                    }, transaction);

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Error creating payment for loan {LoanId}", request.LoanId);
                    throw;
                }
            }

            const string selectSql = @"
                SELECT Id, LoanId, PaymentDate, PrincipalAmount, InterestAmount, PenaltyAmount,
                       TotalPaymentAmount, PaymentMethod, ReferenceNumber, Status, CreatedOn
                FROM [Loans].[LoanPayment]
                WHERE Id = @Id AND IsDeleted = 0";

            var payment = await _connection.QuerySingleOrDefaultAsync<LoanPaymentResponse>(
                selectSql, new { Id = paymentId });

            return payment ?? throw new InvalidOperationException("Payment creation failed");
        }
    }
}
