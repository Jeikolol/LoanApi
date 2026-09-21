using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class DelinquencyPolicyConfig : AuditableConfiguration<DelinquencyPolicy, Guid>
    {
        public DelinquencyPolicyConfig() : base(SchemaNames.Loans, nameof(DelinquencyPolicy))
        {
        }

        public override void Configure(EntityTypeBuilder<DelinquencyPolicy> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.DaysUntilEarlyDelinquency)
                   .IsRequired();

            builder.Property(x => x.EarlyDelinquencyPenalty)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.DaysUntilSevereDelinquency)
                   .IsRequired();

            builder.Property(x => x.SevereDelinquencyPenalty)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.DaysUntilWriteOff)
                   .IsRequired();

            // Late fees and delinquency read "the active policy": the database allows only one.
            builder.HasIndex(x => x.IsActive)
                   .IsUnique()
                   .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
                   .HasDatabaseName("UX_DelinquencyPolicy_OneActive");

            builder.Property(x => x.AutomaticallyChargeLateFees)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(x => x.DailyPenaltyRate)
                   .IsRequired()
                   .HasPrecision(18, 4);
        }
    }
}
