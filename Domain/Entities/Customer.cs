using Domain.Enums;

namespace Domain.Entities
{
    public class Customer : AuditableEntity<Guid>
    {
        public CustomerType CustomerType { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        public Guid IdentificationTypeId { get; set; }
        public IdentificationType IdentificationType { get; set; } = default!;

        public string Identification { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }

        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;

        public Guid CountryId { get; set; }
        public Country Country { get; set; } = default!;

        public Guid NationalityCountryId { get; set; }
        public Country? NationalityCountry { get; set; }

        public CustomerStatus Status { get; set; }

        // Additional required fields for Dominican compliance
        public string Occupation { get; set; } = string.Empty;

        // Financial profile
        public decimal? AnnualIncome { get; set; }
        public IncomeSource? IncomeSource { get; set; }
        public decimal CreditLimit { get; set; }

        // Risk assessment
        public RiskLevel? RiskLevel { get; set; }
        public string? BlacklistReason { get; set; }
        public bool IsBlacklisted { get; set; } = false;

        // Contact details for compliance
        public string? AlternativePhone { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        // Tax ID (RNC - Registro Nacional del Contribuyente)
        public string? TaxId { get; set; }

        // Collections
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
        public CreditScore? CreditScore { get; set; }
    }

}
