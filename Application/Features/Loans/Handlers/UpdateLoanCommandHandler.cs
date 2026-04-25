using Application.Features.Loans.Commands;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class UpdateLoanCommandHandler : IRequestHandler<UpdateLoanCommand, LoanDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<UpdateLoanCommandHandler> _logger;

        public UpdateLoanCommandHandler(IDbConnection connection, ILogger<UpdateLoanCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDetailResponse> Handle(UpdateLoanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating loan {LoanId}", request.Id);

            var loanStatus = request.LoanStatus != null ? Enum.Parse<LoanStatus>(request.LoanStatus) : (LoanStatus?)null;
            var now = DateTime.UtcNow;

            const string updateQuery = @"
                UPDATE [Loans].[Loan]
                SET
                    InterestRate = CASE WHEN @InterestRate IS NOT NULL THEN @InterestRate ELSE InterestRate END,
                    LoanStatus = CASE WHEN @LoanStatus IS NOT NULL THEN @LoanStatus ELSE LoanStatus END,
                    UpdatedOn = @UpdatedOn
                WHERE Id = @Id AND IsDeleted = 0";

            const string selectQuery = @"
                SELECT L.Id, L.LoanNumber, L.CustomerId, C.FirstName + ' ' + C.LastName AS CustomerName,
                       L.PrincipalAmount, L.InterestRate, L.TermMonths, L.InstallmentAmount,
                       L.LoanStatus, L.DisbursementDate, L.MaturityDate, L.NextPaymentDueDate,
                       L.DaysOverdue, L.DelinquencyPercentage, L.PrincipalRemaining,
                       L.BranchId, CUR.CodeIso3 AS CurrencyCode, L.LoanPurpose,
                       L.TotalInterestAmount, L.PaidInterest, L.RemainingInterest,
                       L.AmortizationBalance, L.PrincipalPaid, L.MaxCredit,
                       L.RequiresGuarantor, L.GuarantorId, L.LastPaymentDate,
                       L.CreatedOn, L.UpdatedOn
                FROM [Loans].[Loan] L
                INNER JOIN [Customers].[Customer] C ON L.CustomerId = C.Id
                INNER JOIN [Reference].[Currency] CUR ON L.CurrencyId = CUR.Id
                WHERE L.Id = @Id AND L.IsDeleted = 0";

            try
            {
                var rowsAffected = await _connection.ExecuteAsync(updateQuery, new
                {
                    request.Id,
                    InterestRate = request.InterestRate,
                    LoanStatus = loanStatus.HasValue ? (int)loanStatus.Value : (int?)null,
                    UpdatedOn = now
                });

                if (rowsAffected == 0)
                    throw new KeyNotFoundException($"Loan {request.Id} not found");

                var loan = await _connection.QueryFirstOrDefaultAsync<LoanDetailResponse>(selectQuery, new { Id = request.Id });
                return loan ?? throw new InvalidOperationException("Failed to retrieve updated loan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loan {LoanId}", request.Id);
                throw new InvalidOperationException($"Failed to update loan {request.Id}", ex);
            }
        }
    }
}
