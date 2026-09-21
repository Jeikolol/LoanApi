using Application.Exceptions;
using Application.Features.Documents.Commands;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Documents.Handlers
{
    public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, LoanDocumentResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CreateDocumentCommandHandler> _logger;

        public CreateDocumentCommandHandler(IDbConnection connection, ILogger<CreateDocumentCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDocumentResponse> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating document for loan {LoanId}", request.LoanId);

            var errors = DocumentRules.Validate(request.FileName, request.FileUrl, request.FileSizeBytes);
            if (errors.Count > 0)
                throw new ValidationException("The document is not valid.", errors);

            var loanExists = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM [Loans].[Loan] WHERE Id = @LoanId AND IsDeleted = 0", new { request.LoanId });
            if (loanExists == 0)
                throw new NotFoundException($"Loan {request.LoanId} not found");

            var fileName = request.FileName.Trim();
            DocumentRules.TryGetMimeType(fileName, out var mimeType);
            var documentId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            const string insertQuery = @"
                INSERT INTO [Loans].[LoanDocument] (
                    Id, LoanId, DocumentType, FileName, FileUrl, UploadedDate,
                    FileSizeBytes, MimeType, IsActive, IsDeleted, CreatedOn
                ) VALUES (
                    @Id, @LoanId, @DocumentType, @FileName, @FileUrl, @UploadedDate,
                    @FileSizeBytes, @MimeType, @IsActive, @IsDeleted, @CreatedOn
                )";

            try
            {
                await _connection.ExecuteAsync(insertQuery, new
                {
                    Id = documentId,
                    request.LoanId,
                    DocumentType = (int)request.DocumentType,
                    fileName,
                    request.FileUrl,
                    UploadedDate = now,
                    request.FileSizeBytes,
                    MimeType = (int)mimeType,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedOn = now
                });

                return new LoanDocumentResponse(
                    documentId, request.LoanId, request.DocumentType, fileName,
                    request.FileUrl, now, request.FileSizeBytes, mimeType, DocumentStorage.External
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document for loan {LoanId}", request.LoanId);
                throw new InvalidOperationException($"Failed to create document for loan {request.LoanId}", ex);
            }
        }
    }
}
