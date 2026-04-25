using Application.Features.Delinquency.Commands;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency.Handlers
{
    public class ChargeLateFeeCommandHandler : IRequestHandler<ChargeLateFeeCommand, DelinquencyStatusResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<ChargeLateFeeCommandHandler> _logger;

        public ChargeLateFeeCommandHandler(IDbConnection connection, ILogger<ChargeLateFeeCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<DelinquencyStatusResponse> Handle(ChargeLateFeeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Charging late fees for loan {LoanId}", request.LoanId);

            const string loanSql = @"
                SELECT Id, LoanNumber, DaysOverdue, LoanStatus, InstallmentAmount
                FROM [Loans].[Loan]
                WHERE Id = @LoanId AND IsDeleted = 0";

            var loan = await _connection.QuerySingleOrDefaultAsync<dynamic>(loanSql, new { request.LoanId });
            if (loan == null)
                throw new InvalidOperationException($"Loan {request.LoanId} not found");

            const string policySql = @"
                SELECT TOP 1 DailyPenaltyRate FROM [Loans].[DelinquencyPolicy]
                WHERE IsActive = 1 AND IsDeleted = 0";

            var policy = await _connection.QuerySingleOrDefaultAsync<dynamic>(policySql);
            var lateFee = (decimal?)policy?.DailyPenaltyRate ?? 0.001m;
            var feeAmount = loan.InstallmentAmount * lateFee * Math.Max(1, loan.DaysOverdue);

            const string updateSql = @"
                UPDATE [Loans].[Loan]
                SET AmortizationBalance = AmortizationBalance + @LateFee,
                    RemainingInterest = RemainingInterest + @LateFee,
                    UpdatedOn = @Now
                WHERE Id = @LoanId AND IsDeleted = 0";

            await _connection.ExecuteAsync(updateSql, new
            {
                LateFee = feeAmount,
                request.LoanId,
                Now = DateTime.UtcNow
            });

            return new DelinquencyStatusResponse(
                request.LoanId,
                loan.LoanNumber,
                loan.DaysOverdue,
                (loan.DaysOverdue / 30m) * 100,
                (Domain.Enums.LoanStatus)loan.LoanStatus
            );
        }
    }
}
