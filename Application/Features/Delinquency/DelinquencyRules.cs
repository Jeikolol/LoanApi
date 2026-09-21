using Domain.Enums;

namespace Application.Features.Delinquency
{
    public static class DelinquencyRules
    {
        private static readonly LoanStatus[] CollectableStatuses =
        [
            LoanStatus.Disbursed,
            LoanStatus.Active,
            LoanStatus.PartiallyPaid,
            LoanStatus.Defaulted,
            LoanStatus.Restructured,
        ];

        /// <summary>
        /// Only loans that are being repaid can become (or stop being) delinquent. Pending, approved,
        /// fully paid, written off and closed loans must never be touched by a delinquency calculation.
        /// </summary>
        public static bool IsCollectable(LoanStatus status) => CollectableStatuses.Contains(status);

        public static LoanStatus NextStatus(LoanStatus current, int daysOverdue)
        {
            if (!IsCollectable(current))
            {
                return current;
            }

            // Never WrittenOff from here: writing off books a loss, so an administrator does it on purpose (WriteOffLoanCommand).
            if (daysOverdue > 0) return LoanStatus.Defaulted;

            return current;
        }
    }
}
