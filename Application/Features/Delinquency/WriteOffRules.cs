using Domain.Enums;

namespace Application.Features.Delinquency
{
    /// <summary>
    /// Writing a loan off books the loss, so it is always a deliberate decision by an administrator: a recalculation
    /// never does it. It is only allowed once the loan is past the active policy's write-off period.
    /// </summary>
    public static class WriteOffRules
    {
        public const int DefaultDaysUntilWriteOff = 90;
        public const int MinReasonLength = 5;
        public const int MaxReasonLength = 250;

        /// <summary>Why the loan cannot be written off; null when it can.</summary>
        public static string? Problem(LoanStatus status, decimal principalRemaining, int daysOverdue, int daysUntilWriteOff)
        {
            if (!DelinquencyRules.IsCollectable(status))
            {
                return $"A loan with status {status} cannot be written off.";
            }

            if (principalRemaining <= 0)
            {
                return "The loan has no balance to write off.";
            }

            if (daysOverdue <= daysUntilWriteOff)
            {
                return $"The loan can only be written off after {daysUntilWriteOff} days overdue; this one has {daysOverdue}.";
            }

            return null;
        }
    }
}
