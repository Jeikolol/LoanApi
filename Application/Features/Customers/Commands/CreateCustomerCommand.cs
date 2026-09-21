using Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Customers.Commands
{
    public record CreatedCustomerResponse(Guid Id);

    public class CreateCustomerCommand : IRequest<CreatedCustomerResponse>
    {
        [JsonIgnore]
        public Guid? CreatedById { get; set; }

        public CustomerType CustomerType { get; set; } = CustomerType.PhysicalPerson;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        public Guid IdentificationTypeId { get; set; }
        public string Identification { get; set; } = string.Empty;
        public string? TaxId { get; set; }

        public string Phone { get; set; } = string.Empty;
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }

        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
        public Guid NationalityCountryId { get; set; }

        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public IncomeSource? IncomeSource { get; set; }
        public decimal CreditLimit { get; set; }
        public RiskLevel? RiskLevel { get; set; }

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}
