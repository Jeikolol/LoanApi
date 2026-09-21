using Application.Features.Payments;

namespace Application.Features.Delinquency
{
    /// <summary>What charging late fees up to a day comes to.</summary>
    public record LateFeeAssessment(int DaysCharged, decimal Amount, int InstallmentsOverdue);

    /// <summary>
    /// Late fees accrue per overdue installment and per overdue day: each late installment costs its own amount times
    /// the policy's daily rate for every day it stays unpaid. Every installment-day is charged exactly once, so
    /// charging twice on the same day (or every day) never charges a day twice, and a loan that is several
    /// installments behind pays for each of them.
    /// </summary>
    public static class LateFeeRules
    {
        /// <summary>
        /// How delinquent a loan is: the days overdue times the policy's daily rate, as a percentage (never above 100).
        /// The same rate that prices the late fees, so the two always agree.
        /// </summary>
        public static decimal DelinquencyPercentage(int daysOverdue, decimal dailyRate) =>
            daysOverdue <= 0 ? 0m : Math.Min(100m, Math.Round(daysOverdue * dailyRate * 100m, 2));

        /// <summary>The fee for one day of one late installment.</summary>
        public static decimal DailyFee(decimal installmentAmount, decimal dailyRate) =>
            decimal.Round(installmentAmount * dailyRate, 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// The fees of the days after <paramref name="chargedThrough"/> up to <paramref name="today"/> (all of them when
        /// nothing was charged yet). Installment k falls due on disbursement + k months; the installments the payments
        /// so far cover are never late, and an installment is late on the days after its due date.
        /// </summary>
        public static LateFeeAssessment Assess(
            DateOnly disbursementDate, int termMonths, decimal installmentAmount, decimal principalPaid, decimal interestPaid,
            DateOnly? chargedThrough, DateOnly today, decimal dailyRate)
        {
            int covered = PaymentRules.InstallmentsCovered(principalPaid, interestPaid, installmentAmount, termMonths);
            if (covered >= termMonths || installmentAmount <= 0)
            {
                return new LateFeeAssessment(0, 0m, 0);
            }

            List<DateOnly> unpaidDueDates = [.. Enumerable.Range(covered + 1, termMonths - covered).Select(k => disbursementDate.AddMonths(k))];

            DateOnly firstLateDay = unpaidDueDates[0].AddDays(1);
            DateOnly start = chargedThrough is { } through && through.AddDays(1) > firstLateDay ? through.AddDays(1) : firstLateDay;

            decimal dailyFee = DailyFee(installmentAmount, dailyRate);
            int days = 0;
            decimal amount = 0m;

            for (DateOnly day = start; day <= today; day = day.AddDays(1))
            {
                int late = unpaidDueDates.Count(due => due < day);
                if (late == 0) continue;

                days++;
                amount += dailyFee * late;
            }

            return new LateFeeAssessment(days, amount, unpaidDueDates.Count(due => due < today));
        }
    }
}
