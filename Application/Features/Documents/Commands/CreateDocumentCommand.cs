using Application.Models.Responses;
using Domain.Enums;
using MediatR;

namespace Application.Features.Documents.Commands
{
    public class CreateDocumentCommand : IRequest<LoanDocumentResponse>
    {
        public Guid LoanId { get; set; }
        public DocumentTypes DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }
}
