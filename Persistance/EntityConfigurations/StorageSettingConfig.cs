using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class StorageSettingConfig : AuditableConfiguration<StorageSetting, Guid>
    {
        public StorageSettingConfig() : base(SchemaNames.Admin, nameof(StorageSetting))
        {
        }

        public override void Configure(EntityTypeBuilder<StorageSetting> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.AzureEnabled).IsRequired();
            builder.Property(x => x.AzureAccountName).HasMaxLength(24);
            builder.Property(x => x.AzureContainer).HasMaxLength(63);
        }
    }
}
