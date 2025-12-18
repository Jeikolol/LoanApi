namespace Domain.Entities
{
    public class Role : AuditableEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }  
    }

}
