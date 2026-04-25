using Application.Features.Delinquency.Commands;
using Application.Models.Responses;
using Application.Services;
using Dapper;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency.Handlers
{
    public class CalculateDelinquencyCommandHandler : IRequestHandler<CalculateDelinquencyCommand, DelinquencyStatusResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CalculateDelinquencyCommandHandler> _logger;
        private readonly DelinquencyService _delinquencyService;

        public CalculateDelinquencyCommandHandler(IDbConnection connection, ILogger<CalculateDelinquencyCommandHandler> logger, DelinquencyService delinquencyService)
        {
            _connection = connection;
            _logger = logger;
            _delinquencyService = delinquencyService;
        }

        public async Task<DelinquencyStatusResponse> Handle(CalculateDelinquencyCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Calculating delinquency for loan {LoanId}", request.LoanId);

            const string loanSql = @"
                SELECT Id, LoanNumber, NextPaymentDueDate, PrincipalRemaining, LoanStatus,
                       DaysOverdue, DelinquencyPercentage
                FROM [Loans].[Loan]
                WHERE Id = @LoanId AND IsDeleted = 0";

            var loan = await _connection.QuerySingleOrDefaultAsync<dynamic>(loanSql, new { request.LoanId });
            if (loan == null)
                throw new InvalidOperationException($"Loan {request.LoanId} not found");

            const string policySql = @"
                SELECT TOP 1 Id, DaysUntilEarlyDelinquency, EarlyDelinquencyPenalty,
                       DaysUntilSevereDelinquency, SevereDelinquencyPenalty, DaysUntilWriteOff
                FROM [Loans].[DelinquencyPolicy]
                WHERE IsActive = 1 AND IsDeleted = 0
                ORDER BY CreatedOn DESC";

            var policy = await _connection.QuerySingleOrDefaultAsync<dynamic>(policySql);
            if (policy == null)
                throw new InvalidOperationException("No active delinquency policy found");

            var daysOverdue = (DateTime.UtcNow.Date - loan.NextPaymentDueDate.ToDateTime(TimeOnly.MinValue)).Days;
            var newStatus = daysOverdue > policy.DaysUntilWriteOff ? 7 : (daysOverdue > 0 ? 6 : loan.LoanStatus);

            const string updateSql = @"
                UPDATE [Loans].[Loan]
                SET DaysOverdue = @DaysOverdue,
                    DelinquencyPercentage = @DelinquencyPercentage,
                    LoanStatus = @LoanStatus,
                    UpdatedOn = @Now
                WHERE Id = @LoanId AND IsDeleted = 0";

            await _connection.ExecuteAsync(updateSql, new
            {
                DaysOverdue = Math.Max(0, daysOverdue),
                DelinquencyPercentage = daysOverdue > 0 ? (daysOverdue / 30m) * 100 : 0,
                LoanStatus = newStatus,
                request.LoanId,
                Now = DateTime.UtcNow
            });

            return new DelinquencyStatusResponse(
                request.LoanId,
                loan.LoanNumber,
                Math.Max(0, daysOverdue),
                daysOverdue > 0 ? (daysOverdue / 30m) * 100 : 0,
                (Domain.Enums.LoanStatus)newStatus
            );
        }
    }
}
