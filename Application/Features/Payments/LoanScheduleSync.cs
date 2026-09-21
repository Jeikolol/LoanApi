using Dapper;
using Domain.Enums;
using System.Data;

namespace Application.Features.Payments
{
    /// <summary>
    /// Brings a loan's due date, days overdue and status in line with what has been paid so far. Registering and
    /// reversing a payment both end here, each with its own rule for the status.
    /// </summary>
    internal static class LoanScheduleSync
    {
        /// <summary>
        /// Reads the balances back inside the caller's transaction: the row lock taken by the caller's own update keeps
        /// concurrent payments from computing the schedule from stale values.
        /// </summary>
        public static async Task SyncAsync(
            IDbConnection connection,
            Guid loanId,
            DateOnly today,
            IDbTransaction transaction,
            Func<LoanStatus, decimal, int, LoanStatus> statusRule)
        {
            LoanSchedule loan = await connection.QuerySingleAsync<LoanSchedule>(
                @"SELECT LoanStatus, TermMonths, DisbursementDate, MaturityDate, InstallmentAmount,
                         PrincipalPaid, PaidInterest, PrincipalRemaining
                  FROM [Loans].[Loan] WHERE Id = @LoanId",
                new { LoanId = loanId }, transaction);

            // A loan paid off early (or prepaid beyond its installments) has nothing left to schedule.
            int covered = loan.PrincipalRemaining <= 0
                ? loan.TermMonths
                : PaymentRules.InstallmentsCovered(loan.PrincipalPaid, loan.PaidInterest, loan.InstallmentAmount, loan.TermMonths);
            DateOnly nextDue = PaymentRules.NextDueDate(loan.DisbursementDate, loan.MaturityDate, covered, loan.TermMonths);
            int daysOverdue = loan.PrincipalRemaining <= 0 ? 0 : PaymentRules.DaysOverdue(nextDue, today);
            LoanStatus status = statusRule(loan.LoanStatus, loan.PrincipalRemaining, daysOverdue);

            decimal dailyRate = await connection.QueryFirstOrDefaultAsync<decimal?>(
                @"SELECT DailyPenaltyRate FROM [Loans].[DelinquencyPolicy]
                  WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CreatedOn DESC",
                transaction: transaction) ?? Delinquency.LateFeeCharger.DefaultDailyRate;

            await connection.ExecuteAsync(
                @"UPDATE [Loans].[Loan]
                  SET NextPaymentDueDate = @NextDue,
                      DaysOverdue = @DaysOverdue,
                      DelinquencyPercentage = @Percentage,
                      LoanStatus = @Status
                  WHERE Id = @LoanId",
                new { LoanId = loanId, NextDue = nextDue, DaysOverdue = daysOverdue, Status = (int)status,
                    Percentage = Delinquency.LateFeeRules.DelinquencyPercentage(daysOverdue, dailyRate) },
                transaction);
        }

        private sealed class LoanSchedule
        {
            public LoanStatus LoanStatus { get; set; }
            public int TermMonths { get; set; }
            public DateOnly DisbursementDate { get; set; }
            public DateOnly MaturityDate { get; set; }
            public decimal InstallmentAmount { get; set; }
            public decimal PrincipalPaid { get; set; }
            public decimal PaidInterest { get; set; }
            public decimal PrincipalRemaining { get; set; }
        }
    }
}
