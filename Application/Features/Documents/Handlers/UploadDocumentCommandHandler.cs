using Application.Exceptions;
using Application.Features.Documents.Commands;
using Application.Models.Responses;
using Application.Services;
using Dapper;
using Domain.Enums;
using LoanApi.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net;

namespace Application.Features.Documents.Handlers
{
    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, LoanDocumentResponse>
    {
        private readonly IDbConnection _connection;
        private readonly IFileStorageFactory _storageFactory;
        private readonly ILogger<UploadDocumentCommandHandler> _logger;

        public UploadDocumentCommandHandler(IDbConnection connection, IFileStorageFactory storageFactory, ILogger<UploadDocumentCommandHandler> logger)
        {
            _connection = connection;
            _storageFactory = storageFactory;
            _logger = logger;
        }

        public async Task<LoanDocumentResponse> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            List<string> errors = DocumentRules.ValidateUpload(request.FileName, request.Length);
            if (!Enum.IsDefined(request.DocumentType)) errors.Add("The document type is not recognized.");
            if (errors.Count > 0) throw new ValidationException("The document is not valid.", errors);

            string fileName = request.FileName.Trim();
            DocumentRules.TryGetMimeType(fileName, out MimeTypes mimeType);

            int loans = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM [Loans].[Loan] WHERE Id = @LoanId AND IsDeleted = 0", new { request.LoanId });
            if (loans == 0) throw new NotFoundException($"Loan {request.LoanId} not found");

            // The declared length is not trusted: read at most one byte more than allowed and measure what arrived.
            using MemoryStream buffer = new();
            await request.Content.CopyToAsync(buffer, cancellationToken);
            long actual = buffer.Length;

            List<string> contentErrors = DocumentRules.ValidateUpload(fileName, actual);
            if (contentErrors.Count > 0) throw new ValidationException("The document is not valid.", contentErrors);

            if (!DocumentRules.ContentMatchesType(buffer.GetBuffer().AsSpan(0, (int)Math.Min(actual, 16)), mimeType))
            {
                throw new ValidationException("The document is not valid.", ["The content of the file does not match its type."]);
            }

            IFileStorage storage = await _storageFactory.GetForNewFilesAsync(cancellationToken);
            buffer.Position = 0;

            string key;
            try
            {
                key = await storage.SaveAsync(buffer, Path.GetExtension(fileName), cancellationToken);
            }
            catch (StorageException ex)
            {
                _logger.LogError(ex, "Could not store a document for loan {LoanId}", request.LoanId);
                throw new CustomException(ex.Message, null, HttpStatusCode.BadGateway);
            }

            Guid id = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;
            DocumentStorage provider = storage.Kind == StorageProviderKind.Azure ? DocumentStorage.Azure : DocumentStorage.Local;
            string downloadUrl = $"documents/Download/{id}";

            try
            {
                await _connection.ExecuteAsync(
                    @"INSERT INTO [Loans].[LoanDocument]
                        (Id, LoanId, DocumentType, FileName, FileUrl, UploadedDate, FileSizeBytes, MimeType,
                         StorageProvider, StorageKey, IsActive, IsDeleted, CreatedOn)
                      VALUES
                        (@Id, @LoanId, @DocumentType, @FileName, @FileUrl, @UploadedDate, @FileSizeBytes, @MimeType,
                         @StorageProvider, @StorageKey, 1, 0, @Now)",
                    new
                    {
                        Id = id,
                        request.LoanId,
                        DocumentType = (int)request.DocumentType,
                        FileName = fileName,
                        FileUrl = downloadUrl,
                        UploadedDate = now,
                        FileSizeBytes = actual,
                        MimeType = (int)mimeType,
                        StorageProvider = (int)provider,
                        StorageKey = key,
                        Now = now,
                    });
            }
            catch
            {
                // No row means nothing can ever reach the file, so do not leave it behind.
                await TryDelete(storage, key);
                throw;
            }

            _logger.LogInformation("Stored document {DocumentId} for loan {LoanId} in {Provider}", id, request.LoanId, provider);

            return new LoanDocumentResponse(id, request.LoanId, request.DocumentType, fileName, downloadUrl, now, actual, mimeType, provider);
        }

        private async Task TryDelete(IFileStorage storage, string key)
        {
            try
            {
                await storage.DeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove the orphaned file {Key}", key);
            }
        }
    }
}
