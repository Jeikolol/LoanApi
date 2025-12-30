using Domain.Entities;

namespace Domain
{
    public abstract class AuditableEntity<T>
    {
        public T Id { get; set; } = default!;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public Guid? CreatedById { get; set; }
        public Employee? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedById { get; set; }
        public Employee? UpdatedBy { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedOn { get; set; }
        public Guid? DeletedById { get; set; }
        public Employee? DeletedBy { get; set; }
    }

}
