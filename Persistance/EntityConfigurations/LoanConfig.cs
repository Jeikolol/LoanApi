using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.EntityConfigurations
{
    public class LoanConfig : AuditableConfiguration<Loan, Guid>
    {
        public LoanConfig() : base(SchemaNames.Loans, nameof(Loan))
        {
        }

        public override void Configure(EntityTypeBuilder<Loan> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Customer)
                   .WithMany(x => x.Loans)
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Branch)
                   .WithMany()
                   .HasForeignKey(x => x.BranchId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Currency)
                   .WithMany(x => x.Loans)
                   .HasForeignKey(x => x.CurrencyId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.LoanNumber)
                    .IsRequired();

            builder.Property(x => x.PrincipalAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.InstallmentAmount)
               .IsRequired()
               .HasPrecision(18, 2);

            builder.Property(x => x.InterestRate)
               .IsRequired()
               .HasPrecision(18, 4);

            builder.Property(x => x.TermMonths)
                    .IsRequired();

            builder.Property(x => x.LoanStatus)
                   .IsRequired();

            builder.Property(x => x.DisbursementDate)
                   .IsRequired();

            builder.Property(x => x.MaturityDate)
                   .IsRequired();

            builder.Property(x => x.NextPaymentDueDate)
                   .IsRequired();

            builder.Property(x => x.DaysOverdue)
                   .IsRequired()
                   .HasDefaultValue(0);

            builder.Property(x => x.DelinquencyPercentage)
                   .IsRequired()
                   .HasDefaultValue(0m)
                   .HasPrecision(18, 2);

            builder.Property(x => x.LastPaymentDate)
                   .IsRequired(false);

            builder.Property(x => x.TotalInterestAmount)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.PaidInterest)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.RemainingInterest)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.AmortizationBalance)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.PrincipalPaid)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.PrincipalRemaining)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.LoanPurpose)
                   .IsRequired(false);

            builder.Property(x => x.MaxCredit)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(x => x.RequiresGuarantor)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(x => x.GuarantorId)
                   .IsRequired(false);

            builder.HasMany(x => x.Payments)
                   .WithOne(x => x.Loan)
                   .HasForeignKey(x => x.LoanId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Documents)
                   .WithOne(x => x.Loan)
                   .HasForeignKey(x => x.LoanId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
