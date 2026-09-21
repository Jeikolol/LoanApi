namespace Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public bool Succeeded { get; set; }
        public string? Error { get; set; }
        public string? IpAddress { get; set; }
        public int DurationMs { get; set; }
    }
}
