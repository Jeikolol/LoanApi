namespace Domain.Entities
{
    public class Country : AuditableEntity<Guid>
    {
        public string? Code { get; set; }
        public string CodeIso2 { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;

        /// <summary>International dial code without the plus, for example "1" or "57".</summary>
        public string? PhoneCode { get; set; }

        /// <summary>Shape of the national number, where 9 stands for a digit, for example "999-999-9999".</summary>
        public string? PhoneMask { get; set; }

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Customer> CustomersNationality { get; set; } = new List<Customer>();
    }
}
