using Application.Exceptions;
using Application.Features.Documents.Queries;
using Application.Services;
using Dapper;
using Domain.Enums;
using LoanApi.Storage;
using MediatR;
using System.Data;
using System.Net;

namespace Application.Features.Documents.Handlers
{
    public class GetDocumentFileQueryHandler : IRequestHandler<GetDocumentFileQuery, DocumentFileResult>
    {
        private readonly IDbConnection _connection;
        private readonly IFileStorageFactory _storageFactory;

        public GetDocumentFileQueryHandler(IDbConnection connection, IFileStorageFactory storageFactory)
        {
            _connection = connection;
            _storageFactory = storageFactory;
        }

        public async Task<DocumentFileResult> Handle(GetDocumentFileQuery request, CancellationToken cancellationToken)
        {
            DocumentRow document = await _connection.QuerySingleOrDefaultAsync<DocumentRow>(
                @"SELECT FileName, MimeType, StorageProvider, StorageKey
                  FROM [Loans].[LoanDocument] WHERE Id = @Id AND IsDeleted = 0",
                new { request.Id })
                ?? throw new NotFoundException($"Document {request.Id} not found");

            if (document.StorageProvider == DocumentStorage.External || string.IsNullOrEmpty(document.StorageKey))
            {
                throw new ConflictException("This document is an external reference; open its link instead.");
            }

            try
            {
                IFileStorage storage = await _storageFactory.GetForExistingAsync(document.StorageProvider, cancellationToken);
                Stream content = await storage.OpenReadAsync(document.StorageKey, cancellationToken);

                return new DocumentFileResult(content, document.FileName, DocumentRules.ContentType(document.MimeType));
            }
            catch (FileNotFoundException)
            {
                throw new NotFoundException("The file is no longer in the storage.");
            }
            catch (StorageException ex)
            {
                throw new CustomException(ex.Message, null, HttpStatusCode.BadGateway);
            }
        }

        private sealed class DocumentRow
        {
            public string FileName { get; set; } = string.Empty;
            public MimeTypes MimeType { get; set; }
            public DocumentStorage StorageProvider { get; set; }
            public string? StorageKey { get; set; }
        }
    }
}
