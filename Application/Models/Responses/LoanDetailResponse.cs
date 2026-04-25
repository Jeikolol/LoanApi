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
        decimal AmortizationBalance,
        decimal PrincipalPaid,
        decimal MaxCredit,
        bool RequiresGuarantor,
        Guid? GuarantorId,
        DateTime? LastPaymentDate,
        DateTime CreatedOn,
        DateTime? UpdatedOn
    );
}
