using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class CreditScoreConfig : AuditableConfiguration<CreditScore, Guid>
    {
        public CreditScoreConfig() : base(SchemaNames.Customers, nameof(CreditScore))
        {
        }

        public override void Configure(EntityTypeBuilder<CreditScore> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Customer)
                   .WithOne(x => x.CreditScore)
                   .HasForeignKey<CreditScore>(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Score)
                   .IsRequired();

            builder.Property(x => x.PaymentHistoryScore)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.CreditUtilizationScore)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.CreditAgeScore)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.DefaultHistoryScore)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.CreditRating)
                   .IsRequired();

            builder.Property(x => x.LastCalculatedDate)
                   .IsRequired();
        }
    }
}
