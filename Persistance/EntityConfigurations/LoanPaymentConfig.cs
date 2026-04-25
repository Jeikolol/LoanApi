using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class LoanPaymentConfig : AuditableConfiguration<LoanPayment, Guid>
    {
        public LoanPaymentConfig() : base(SchemaNames.Loans, nameof(LoanPayment))
        {
        }

        public override void Configure(EntityTypeBuilder<LoanPayment> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Loan)
                   .WithMany(x => x.Payments)
                   .HasForeignKey(x => x.LoanId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Currency)
                   .WithMany(x => x.LoanPayments)
                   .HasForeignKey(x => x.CurrencyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.PaymentDate)
                   .IsRequired();

            builder.Property(x => x.PrincipalAmount)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.InterestAmount)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.PenaltyAmount)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.TotalPaymentAmount)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.PaymentMethod)
                   .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                   .IsRequired(false)
                   .HasMaxLength(100);

            builder.Property(x => x.Status)
                   .IsRequired();
        }
    }
}
