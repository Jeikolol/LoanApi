using Domain.Enums;

namespace Application.Models.Responses
{
    public record LoanPaymentResponse(
        Guid Id,
        Guid LoanId,
        DateTime PaymentDate,
        decimal PrincipalAmount,
        decimal InterestAmount,
        decimal PenaltyAmount,
        decimal TotalPaymentAmount,
        string PaymentMethod,
        string? ReferenceNumber,
        PaymentStatus Status,
        DateTime CreatedOn
    );
}
