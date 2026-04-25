using Domain.Enums;

namespace Application.Models.Responses
{
    public record LoanSummaryResponse(
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
        decimal PrincipalRemaining
    );
}
