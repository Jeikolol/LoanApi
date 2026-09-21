using Application.Exceptions;
using Application.Features.Delinquency.Commands;
using Application.Models.Responses;
using Application.Services;
using Dapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency.Handlers
{
    internal record CalculateLoanQueryResult(Guid Id, string LoanNumber, DateOnly NextPaymentDueDate, decimal PrincipalRemaining, int LoanStatus, int DaysOverdue, decimal DelinquencyPercentage);
    internal record CalculatePolicyQueryResult(Guid Id, int DaysUntilEarlyDelinquency, decimal EarlyDelinquencyPenalty, int DaysUntilSevereDelinquency, decimal SevereDelinquencyPenalty, int DaysUntilWriteOff, decimal DailyPenaltyRate);

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

            var loan = await _connection.QuerySingleOrDefaultAsync<CalculateLoanQueryResult>(loanSql, new { request.LoanId });
            if (loan == null)
                throw new NotFoundException($"Loan {request.LoanId} not found");

            if (!DelinquencyRules.IsCollectable((LoanStatus)loan.LoanStatus))
            {
                return new DelinquencyStatusResponse(
                    request.LoanId, loan.LoanNumber, loan.DaysOverdue, loan.DelinquencyPercentage, (LoanStatus)loan.LoanStatus);
            }

            const string policySql = @"
                SELECT TOP 1 Id, DaysUntilEarlyDelinquency, EarlyDelinquencyPenalty,
                       DaysUntilSevereDelinquency, SevereDelinquencyPenalty, DaysUntilWriteOff, DailyPenaltyRate
                FROM [Loans].[DelinquencyPolicy]
                WHERE IsActive = 1 AND IsDeleted = 0
                ORDER BY CreatedOn DESC";

            var policy = await _connection.QuerySingleOrDefaultAsync<CalculatePolicyQueryResult>(policySql);
            if (policy == null)
                throw new ConflictException("No active delinquency policy is configured.");

            var daysOverdue = (DateTime.UtcNow.Date - loan.NextPaymentDueDate.ToDateTime(TimeOnly.MinValue)).Days;
            decimal percentage = LateFeeRules.DelinquencyPercentage(daysOverdue, policy.DailyPenaltyRate);
            var newStatus = (int)DelinquencyRules.NextStatus((LoanStatus)loan.LoanStatus, daysOverdue);

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
                DelinquencyPercentage = percentage,
                LoanStatus = newStatus,
                request.LoanId,
                Now = DateTime.UtcNow
            });

            return new DelinquencyStatusResponse(
                request.LoanId,
                loan.LoanNumber,
                Math.Max(0, daysOverdue),
                percentage,
                (LoanStatus)newStatus
            );
        }
    }
}
