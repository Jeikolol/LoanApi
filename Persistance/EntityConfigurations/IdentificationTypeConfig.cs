using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class IdentificationTypeConfig : AuditableConfiguration<IdentificationType, Guid>
    {
        public IdentificationTypeConfig() : base(SchemaNames.Admin, nameof(IdentificationType))
        {
        }

        public override void Configure(EntityTypeBuilder<IdentificationType> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.Code)
                   .IsUnique();

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Description)
                   .IsRequired(false)
                   .HasMaxLength(500);
        }
    }
}
