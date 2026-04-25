using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.EntityConfigurations
{
    public class LoanDocumentConfig : AuditableConfiguration<LoanDocument, Guid>
    {
        public LoanDocumentConfig() : base(SchemaNames.Loans, nameof(LoanDocument))
        {
        }

        public override void Configure(EntityTypeBuilder<LoanDocument> builder)
        {
            base.Configure(builder);

            builder.HasOne(x => x.Loan)
                   .WithMany(x => x.Documents)
                   .HasForeignKey(x => x.LoanId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.DocumentType)
                   .IsRequired();

            builder.Property(x => x.FileName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(x => x.FileUrl)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(x => x.UploadedDate)
                   .IsRequired();

            builder.Property(x => x.FileSizeBytes)
                   .IsRequired();

            builder.Property(x => x.MimeType)
                   .IsRequired();
        }
    }
}
