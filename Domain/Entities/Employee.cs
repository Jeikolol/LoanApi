namespace Domain.Entities
{
    public class Employee : AuditableEntity<Guid>
    {
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = default!;

        public Guid BranchId { get; set; }
        public Branch Branch { get; set; } = default!;

        public DateTime? LastLoginOn { get; set; }

        public Employee? CreatedBy { get; set; }
        public Employee? UpdatedBy { get; set; }
        public Employee? DeletedBy { get; set; }
    }

}
