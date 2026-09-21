using Domain.Enums;

namespace Application.Features.Loans
{
    /// <summary>
    /// The manual life cycle of a loan: Pending -> Approved -> Disbursed. From there payments (and the delinquency
    /// calculation) drive it to Active, Defaulted and FullyPaid, and a settled loan can be Closed.
    /// A loan that has not been disbursed can be cancelled.
    /// </summary>
    public static class LoanLifecycleRules
    {
        /// <summary>Loans that are owed money, and therefore count against the customer's credit limit.</summary>
        private static readonly LoanStatus[] ExposureStatuses =
        [
            LoanStatus.Approved,
            LoanStatus.Disbursed,
            LoanStatus.Active,
            LoanStatus.PartiallyPaid,
            LoanStatus.Defaulted,
            LoanStatus.Restructured,
        ];

        public const int MaxDaysOverdueToBorrowAgain = 30;

        public static IReadOnlyList<LoanStatus> StatusesCountedAsExposure => ExposureStatuses;

        /// <summary>The status the loan moves to, or null when the action is not allowed from the current status.</summary>
        public static LoanStatus? Apply(LoanStatus current, LoanAction action) => (current, action) switch
        {
            (LoanStatus.Pending, LoanAction.Approve) => LoanStatus.Approved,
            (LoanStatus.Approved, LoanAction.Disburse) => LoanStatus.Disbursed,
            (LoanStatus.Pending or LoanStatus.Approved, LoanAction.Cancel) => LoanStatus.Closed,
            (LoanStatus.FullyPaid, LoanAction.Close) => LoanStatus.Closed,
            _ => null,
        };

        public static IReadOnlyList<LoanAction> AvailableActions(LoanStatus current) =>
            [.. Enum.GetValues<LoanAction>().Where(action => Apply(current, action) is not null)];

        /// <summary>
        /// Whoever creates a loan cannot approve it, whatever their role. Loans with no recorded creator
        /// (for example the imported ones) can be approved by anyone allowed to.
        /// </summary>
        public static string? ApprovalProblem(Guid? createdById, Guid? approverId) =>
            createdById is not null && createdById == approverId
                ? "A loan cannot be approved by the employee who created it."
                : null;

        /// <summary>Why a customer cannot be given (more) credit; empty when they can.</summary>
        public static List<string> EligibilityProblems(
            bool isBlacklisted, CustomerStatus customerStatus, decimal creditLimit,
            decimal openExposure, int maxDaysOverdue, decimal principal)
        {
            List<string> problems = [];

            if (isBlacklisted) problems.Add("The customer is blacklisted.");
            if (customerStatus != CustomerStatus.Active) problems.Add($"The customer is {customerStatus}, not Active.");
            if (maxDaysOverdue > MaxDaysOverdueToBorrowAgain)
            {
                problems.Add($"The customer has a loan more than {MaxDaysOverdueToBorrowAgain} days overdue.");
            }

            if (openExposure + principal > creditLimit)
            {
                problems.Add($"The loan would take the customer's outstanding credit to {openExposure + principal:N2}, above the credit limit of {creditLimit:N2}.");
            }

            return problems;
        }
    }
}
