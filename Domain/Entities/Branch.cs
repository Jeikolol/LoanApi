namespace Domain.Entities
{
    public class Branch : AuditableEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
    }

}
