namespace Domain.Entities
{
    public class Country : AuditableEntity<Guid>
    {
        public string? Code { get; set; }
        public string CodeIso2 { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();
        public ICollection<Customer> CustomersNationality { get; set; } = new List<Customer>();
    }
}
