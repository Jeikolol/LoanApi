using MediatR;

namespace Application.Features.Documents.Queries
{
    public sealed class DocumentFileResult : IAsyncDisposable
    {
        public DocumentFileResult(Stream content, string fileName, string contentType)
        {
            Content = content;
            FileName = fileName;
            ContentType = contentType;
        }

        public Stream Content { get; }
        public string FileName { get; }
        public string ContentType { get; }

        public ValueTask DisposeAsync() => Content.DisposeAsync();
    }

    /// <summary>Opens the stored file behind a document (not available for external references).</summary>
    public record GetDocumentFileQuery(Guid Id) : IRequest<DocumentFileResult>;
}
