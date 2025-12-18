namespace Domain.Entities
{
    public class EmployeeSession
    {
        public Guid SessionId { get; set; }
        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = default!;

        public DateTime LogInOn { get; set; }
        public DateTime? LogOutOn { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
