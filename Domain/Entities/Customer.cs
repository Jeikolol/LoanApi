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
        public string Country { get; set; } = "DO";

        public CustomerStatus Status { get; set; }

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }

}
