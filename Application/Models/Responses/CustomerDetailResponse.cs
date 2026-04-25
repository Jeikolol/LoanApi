using Domain.Enums;

namespace Application.Models.Responses
{
    public record CustomerDetailResponse(
        Guid Id,
        CustomerType CustomerType,
        string FirstName,
        string LastName,
        string? CompanyName,
        string Identification,
        Guid IdentificationTypeId,
        string Phone,
        string? Email,
        string Address,
        string Province,
        Guid CountryId,
        CustomerStatus Status,
        string Occupation,
        decimal? AnnualIncome,
        IncomeSource? IncomeSource,
        decimal CreditLimit,
        RiskLevel? RiskLevel,
        bool IsBlacklisted,
        string? BlacklistReason,
        string? TaxId,
        string? AlternativePhone,
        string? EmergencyContactName,
        string? EmergencyContactPhone,
        DateTime CreatedOn,
        DateTime? UpdatedOn
    );
}
