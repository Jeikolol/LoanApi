using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class EmployeesConfig : AuditableConfiguration<Employee, Guid>
    {
        public EmployeesConfig() : base(tableName: nameof(Employee))
        {
        }

        public override void Configure(EntityTypeBuilder<Employee> builder)
        {
            base.Configure(builder);

            // =========================
            // Core identity
            // =========================
            builder.Property(x => x.UserName)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasIndex(x => x.UserName)
                   .IsUnique();

            builder.Property(x => x.PasswordHash)
                   .IsRequired();

            builder.Property(x => x.PasswordSalt)
                   .IsRequired();

            // =========================
            // Personal data
            // =========================
            builder.Property(x => x.FirstName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.LastName)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Email)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.HasIndex(x => x.Email)
                   .IsUnique();

            // =========================
            // Relationships
            // =========================
            builder.HasOne(x => x.Role)
                   .WithMany()
                   .HasForeignKey(x => x.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Branch)
                   .WithMany()
                   .HasForeignKey(x => x.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);          

            // =========================
            // Operational fields
            // =========================
            builder.Property(x => x.LastLoginOn)
                   .IsRequired(false);
        }
    }
}
