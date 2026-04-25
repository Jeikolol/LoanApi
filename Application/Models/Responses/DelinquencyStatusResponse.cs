using Domain.Enums;

namespace Application.Models.Responses
{
    public record DelinquencyStatusResponse(
        Guid LoanId,
        string LoanNumber,
        int DaysOverdue,
        decimal DelinquencyPercentage,
        LoanStatus LoanStatus
    );
}
