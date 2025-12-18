using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public abstract class AuditableEntity<T>
    {
        public T Id { get; set; } = default!;

        public DateTime CreatedOn { get; set; }
        public Guid CreatedById { get; set; }
        public Employee CreatedBy { get; set; } = default!;

        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedById { get; set; }
        public Employee? UpdatedBy { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        public Guid? DeletedById { get; set; }
        public Employee? DeletedBy { get; set; }
    }

}
