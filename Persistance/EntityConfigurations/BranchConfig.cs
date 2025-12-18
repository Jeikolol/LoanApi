using Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Persistence.EntityConfigurations
{
    public class BranchConfig : AuditableConfiguration<Branch, Guid>
    {
        public BranchConfig() : base(tableName: nameof(Branch))
        {
        }

        public override void Configure(EntityTypeBuilder<Branch> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Code)
                   .HasMaxLength(50)
                   .IsRequired();
            builder.HasIndex(x => x.Code);

            builder.Property(x => x.Name)
                   .IsRequired();

            builder.Property(x => x.Address);
            builder.Property(x => x.City);
        }
    }
}
