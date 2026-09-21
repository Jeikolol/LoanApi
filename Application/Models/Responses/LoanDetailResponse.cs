using Domain.Enums;

namespace Application.Models.Responses
{
    public record LoanDetailResponse(
        Guid Id,
        string LoanNumber,
        Guid CustomerId,
        string CustomerName,
        decimal PrincipalAmount,
        decimal InterestRate,
        int TermMonths,
        decimal InstallmentAmount,
        LoanStatus LoanStatus,
        DateTime DisbursementDate,
        DateTime MaturityDate,
        DateTime NextPaymentDueDate,
        int DaysOverdue,
        decimal DelinquencyPercentage,
        decimal PrincipalRemaining,
        Guid BranchId,
        string CurrencyCode,
        LoanPurpose? LoanPurpose,
        decimal TotalInterestAmount,
        decimal PaidInterest,
        decimal RemainingInterest,
        decimal LateFeesCharged,
        decimal LateFeesPaid,
        decimal AmortizationBalance,
        decimal PrincipalPaid,
        decimal MaxCredit,
        bool RequiresGuarantor,
        Guid? GuarantorId,
        DateTime? LastPaymentDate,
        DateTime CreatedOn,
        DateTime? UpdatedOn
    )
    {
        /// <summary>Only filled when the loan is read on its own; it is not part of the constructor Dapper matches.</summary>
        public string? BranchName { get; init; }
    }
}
