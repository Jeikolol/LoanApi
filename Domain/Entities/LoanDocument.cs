using Domain.Enums;

namespace Domain.Entities
{
    public class LoanDocument : AuditableEntity<Guid>
    {
        public Guid LoanId { get; set; }
        public Loan Loan { get; set; } = default!;

        public DocumentTypes DocumentType { get; set; } = DocumentTypes.Contract;
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public long FileSizeBytes { get; set; }
        public MimeTypes MimeType { get; set; } = MimeTypes.Pdf;
    }
}
