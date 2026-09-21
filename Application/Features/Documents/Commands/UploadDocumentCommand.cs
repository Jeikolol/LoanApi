using Application.Models.Responses;
using Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Application.Features.Documents.Commands
{
    /// <summary>Stores an uploaded file for a loan in the active storage (Azure when enabled, local otherwise).</summary>
    public class UploadDocumentCommand : IRequest<LoanDocumentResponse>
    {
        public Guid LoanId { get; set; }
        public DocumentTypes DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;

        /// <summary>The size the client declared; the handler enforces the real one while reading.</summary>
        public long Length { get; set; }

        /// <summary>The file itself. It is never written to the audit trail.</summary>
        [JsonIgnore]
        public Stream Content { get; set; } = Stream.Null;
    }
}
