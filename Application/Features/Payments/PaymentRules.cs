using Application.Features.Delinquency;
using Domain.Enums;

namespace Application.Features.Payments
{
    /// <summary>
    /// How a payment moves a loan along its schedule. Installment k falls due on DisbursementDate + k months
    /// and each one costs InstallmentAmount of principal plus interest (penalties do not count towards it).
    /// </summary>
    public static class PaymentRules
    {
        public static int InstallmentsCovered(decimal principalPaid, decimal interestPaid, decimal installmentAmount, int termMonths)
        {
            if (installmentAmount <= 0 || termMonths <= 0) return 0;

            int covered = (int)Math.Floor((principalPaid + interestPaid) / installmentAmount);
            return Math.Clamp(covered, 0, termMonths);
        }

        public static DateOnly NextDueDate(DateOnly disbursementDate, DateOnly maturityDate, int installmentsCovered, int termMonths)
        {
            return installmentsCovered >= termMonths ? maturityDate : disbursementDate.AddMonths(installmentsCovered + 1);
        }

        public static int DaysOverdue(DateOnly nextDueDate, DateOnly today)
        {
            return Math.Max(0, today.DayNumber - nextDueDate.DayNumber);
        }

        /// <summary>Only loans that are still being repaid (or were just settled) can have a payment reversed; closed and written-off ones are final.</summary>
        public static bool CanReversePaymentsOf(LoanStatus status) =>
            status is LoanStatus.Disbursed or LoanStatus.Active or LoanStatus.PartiallyPaid
                or LoanStatus.Defaulted or LoanStatus.Restructured or LoanStatus.FullyPaid;

        /// <summary>
        /// Reversing a payment can un-settle a loan (FullyPaid goes back to being repaid) and can leave it behind
        /// schedule again, in which case it is delinquent from that moment, not only when the next calculation runs.
        /// </summary>
        public static LoanStatus StatusAfterReversal(LoanStatus current, decimal principalRemaining, int daysOverdue)
        {
            if (!CanReversePaymentsOf(current) || principalRemaining <= 0) return current;

            if (daysOverdue > 0) return LoanStatus.Defaulted;
            return current == LoanStatus.FullyPaid ? LoanStatus.Active : current;
        }

        /// <summary>
        /// A payment settles the loan when nothing is left, brings a delinquent loan back to Active once it is
        /// up to date, and activates a freshly disbursed one. It never makes a loan delinquent: that is the
        /// delinquency calculation's job.
        /// </summary>
        public static LoanStatus StatusAfterPayment(LoanStatus current, decimal principalRemaining, int daysOverdue)
        {
            if (principalRemaining <= 0) return LoanStatus.FullyPaid;
            if (!DelinquencyRules.IsCollectable(current)) return current;

            if (current is LoanStatus.Defaulted or LoanStatus.Disbursed && daysOverdue == 0) return LoanStatus.Active;

            return current;
        }
    }
}
