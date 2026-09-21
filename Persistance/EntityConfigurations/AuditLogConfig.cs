using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable(nameof(AuditLog), SchemaNames.Admin);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserName).HasMaxLength(100);
            builder.Property(x => x.Action).HasMaxLength(200).IsRequired();
            builder.Property(x => x.Payload).HasMaxLength(4000);
            builder.Property(x => x.Error).HasMaxLength(500);
            builder.Property(x => x.IpAddress).HasMaxLength(64);

            builder.HasIndex(x => x.OccurredOn);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Action);
        }
    }
}
