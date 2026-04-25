using Domain.Enums;

namespace Application.Models.Responses
{
    public record LoanDocumentResponse(
        Guid Id,
        Guid LoanId,
        DocumentTypes DocumentType,
        string FileName,
        string FileUrl,
        DateTime UploadedDate,
        long FileSizeBytes,
        string MimeType
    );
}
