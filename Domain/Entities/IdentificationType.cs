namespace Domain.Entities
{
    public class IdentificationType : AuditableEntity<Guid>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
    }

}
