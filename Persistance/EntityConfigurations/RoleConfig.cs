using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class RoleConfig : AuditableConfiguration<Role, Guid>
    {
        public RoleConfig() : base(tableName: nameof(Role))
        {
        }

        public override void Configure(EntityTypeBuilder<Role> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Code)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.HasIndex(x => x.Code);

            builder.Property(x => x.Name)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .IsRequired(false);
        }
    }
}
