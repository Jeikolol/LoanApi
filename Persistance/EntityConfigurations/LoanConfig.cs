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
        public LoanConfig() : base(tableName: nameof(Loan))
        {
        }

        public override void Configure(EntityTypeBuilder<Loan> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Branch)
                   .WithMany()
                   .HasForeignKey(x => x.BranchId)
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
        }
    }
}
