namespace Application.Models.Responses
{
    public record IdentificationTypeResponse(Guid Id, string Code, string Name, string Mask);

    public record CountryResponse(Guid Id, string CodeIso2, string Name, string Nationality, string? PhoneCode, string? PhoneMask);
}
