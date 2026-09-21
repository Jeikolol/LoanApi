using Application.Features.Payments;

namespace Application.Features.Dashboard
{
    public record HistoryLoan(Guid Id, DateOnly DisbursementDate, DateOnly MaturityDate, int TermMonths, decimal InstallmentAmount);

    /// <summary>A payment's effect on the schedule (principal + interest); it counts from its date until it is reversed, if ever.</summary>
    public record HistoryPayment(Guid LoanId, DateOnly PaymentDate, decimal Amount, DateOnly? ReversedOn);

    /// <summary>
    /// Rebuilds how delinquent the portfolio was on a past day from the loans' schedules and the payments made up to
    /// that day (nothing stores daily snapshots): a loan's days overdue then is how long its first unpaid installment
    /// had been due.
    /// </summary>
    public static class DelinquencyHistory
    {
        /// <summary>Average days overdue over the loans already disbursed on <paramref name="day"/> (0 when there are none).</summary>
        public static decimal AverageDaysOverdue(DateOnly day, IReadOnlyList<HistoryLoan> loans, ILookup<Guid, HistoryPayment> payments)
        {
            int count = 0;
            long totalDays = 0;

            foreach (HistoryLoan loan in loans)
            {
                if (loan.DisbursementDate > day) continue;

                count++;
                decimal paid = payments[loan.Id]
                    .Where(p => p.PaymentDate <= day && (p.ReversedOn is null || p.ReversedOn > day))
                    .Sum(p => p.Amount);

                int covered = PaymentRules.InstallmentsCovered(paid, 0, loan.InstallmentAmount, loan.TermMonths);
                if (covered >= loan.TermMonths) continue; // settled: nothing overdue

                totalDays += PaymentRules.DaysOverdue(PaymentRules.NextDueDate(loan.DisbursementDate, loan.MaturityDate, covered, loan.TermMonths), day);
            }

            return count == 0 ? 0m : Math.Round((decimal)totalDays / count, 2);
        }

        /// <summary>The last <paramref name="days"/> days ending today, oldest first, against the same days one period earlier.</summary>
        public static (string[] Labels, decimal[] Current, decimal[] Previous) Trend(
            DateOnly today, int days, IReadOnlyList<HistoryLoan> loans, IReadOnlyList<HistoryPayment> payments)
        {
            ILookup<Guid, HistoryPayment> byLoan = payments.ToLookup(p => p.LoanId);

            List<DateOnly> range = [.. Enumerable.Range(0, days).Select(i => today.AddDays(i - days + 1))];

            return (
                [.. range.Select(d => d.ToString("dd/MM"))],
                [.. range.Select(d => AverageDaysOverdue(d, loans, byLoan))],
                [.. range.Select(d => AverageDaysOverdue(d.AddDays(-days), loans, byLoan))]);
        }
    }
}
