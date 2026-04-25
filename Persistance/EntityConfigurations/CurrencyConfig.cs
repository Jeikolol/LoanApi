using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class CurrencyConfig : AuditableConfiguration<Currency, Guid>
    {
        public CurrencyConfig() : base(SchemaNames.Reference, nameof(Currency))
        {
        }

        public override void Configure(EntityTypeBuilder<Currency> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CodeIso2)
                   .IsRequired()
                   .HasMaxLength(2);

            builder.Property(x => x.CodeIso3)
                   .IsRequired()
                   .HasMaxLength(3);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Symbol)
                   .IsRequired()
                   .HasMaxLength(10);

            builder.Property(x => x.IsDefault)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.HasMany(x => x.Loans)
                   .WithOne(x => x.Currency)
                   .HasForeignKey(x => x.CurrencyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.LoanPayments)
                   .WithOne(x => x.Currency)
                   .HasForeignKey(x => x.CurrencyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
