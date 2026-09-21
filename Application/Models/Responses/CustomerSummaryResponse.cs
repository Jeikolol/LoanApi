using Domain.Enums;

namespace Application.Models.Responses
{
    public record CustomerSummaryResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string Identification,
        CustomerType CustomerType,
        string Phone,
        string? Email,
        CustomerStatus Status,
        RiskLevel? RiskLevel,
        decimal CreditLimit,
        bool IsBlacklisted
    );
}
