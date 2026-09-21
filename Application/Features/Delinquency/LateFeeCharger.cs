using Application.Exceptions;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency
{
    public interface ILateFeeCharger
    {
        /// <summary>Charges the overdue days of one loan that have not been charged yet. Repeating it the same day charges nothing.</summary>
        Task<LateFeeChargeResponse> ChargeAsync(Guid loanId, DateOnly today, CancellationToken cancellationToken = default);
    }

    /// <summary>The one place a late fee is charged: the manual action, the "charge all" action and the daily job all end here.</summary>
    public sealed class LateFeeCharger : ILateFeeCharger
    {
        /// <summary>Used when no delinquency policy is active.</summary>
        public const decimal DefaultDailyRate = 0.001m;

        private readonly IDbConnection _connection;
        private readonly ILogger<LateFeeCharger> _logger;

        public LateFeeCharger(IDbConnection connection, ILogger<LateFeeCharger> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LateFeeChargeResponse> ChargeAsync(Guid loanId, DateOnly today, CancellationToken cancellationToken = default)
        {
            LoanRow loan = await _connection.QuerySingleOrDefaultAsync<LoanRow>(
                @"SELECT LoanNumber, LoanStatus, NextPaymentDueDate, InstallmentAmount, LateFeesCharged, LateFeesChargedThrough,
                         DisbursementDate, TermMonths, PrincipalPaid, PaidInterest
                  FROM [Loans].[Loan] WHERE Id = @LoanId AND IsDeleted = 0",
                new { LoanId = loanId })
                ?? throw new NotFoundException($"Loan {loanId} not found");

            if (loan.NextPaymentDueDate >= today || !DelinquencyRules.IsCollectable(loan.LoanStatus))
            {
                throw new ConflictException("Late fees can only be charged to an overdue loan that is being collected.");
            }

            decimal dailyRate = await _connection.QueryFirstOrDefaultAsync<decimal?>(
                @"SELECT DailyPenaltyRate FROM [Loans].[DelinquencyPolicy]
                  WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CreatedOn DESC") ?? DefaultDailyRate;

            LateFeeAssessment assessment = LateFeeRules.Assess(
                loan.DisbursementDate, loan.TermMonths, loan.InstallmentAmount, loan.PrincipalPaid, loan.PaidInterest,
                loan.LateFeesChargedThrough, today, dailyRate);

            int days = assessment.DaysCharged;
            decimal amount = assessment.Amount;
            if (days == 0)
            {
                // Everything up to today is charged already: repeating the request is harmless and adds nothing.
                return new LateFeeChargeResponse(loanId, loan.LoanNumber, 0, 0m, loan.LateFeesCharged, loan.LateFeesChargedThrough ?? today);
            }

            // The guard makes it atomic: two requests racing for the same days cannot both charge them.
            int updated = await _connection.ExecuteAsync(
                @"UPDATE [Loans].[Loan]
                  SET LateFeesCharged = LateFeesCharged + @Amount, LateFeesChargedThrough = @Today, UpdatedOn = @Now
                  WHERE Id = @LoanId AND IsDeleted = 0
                    AND (LateFeesChargedThrough IS NULL OR LateFeesChargedThrough < @Today)",
                new { LoanId = loanId, Amount = amount, Today = today, Now = DateTime.UtcNow });

            if (updated != 1)
            {
                throw new ConflictException("The late fees were charged in the meantime. Please refresh.");
            }

            _logger.LogInformation("Charged {Days} day(s) of late fees ({Amount}) to loan {LoanId}", days, amount, loanId);

            return new LateFeeChargeResponse(loanId, loan.LoanNumber, days, amount, loan.LateFeesCharged + amount, today);
        }

        private sealed class LoanRow
        {
            public string LoanNumber { get; set; } = string.Empty;
            public LoanStatus LoanStatus { get; set; }
            public DateOnly NextPaymentDueDate { get; set; }
            public decimal InstallmentAmount { get; set; }
            public DateOnly DisbursementDate { get; set; }
            public int TermMonths { get; set; }
            public decimal PrincipalPaid { get; set; }
            public decimal PaidInterest { get; set; }
            public decimal LateFeesCharged { get; set; }
            public DateOnly? LateFeesChargedThrough { get; set; }
        }
    }
}
