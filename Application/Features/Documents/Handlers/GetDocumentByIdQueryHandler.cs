using Application.Exceptions;
using Application.Features.Documents.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Documents.Handlers
{
    public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, LoanDocumentResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetDocumentByIdQueryHandler> _logger;

        public GetDocumentByIdQueryHandler(IDbConnection connection, ILogger<GetDocumentByIdQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDocumentResponse> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting document by ID: {DocumentId}", request.Id);

            const string query = @"
                SELECT LD.Id, LD.LoanId, LD.DocumentType, LD.FileName, LD.FileUrl,
                       LD.UploadedDate, LD.FileSizeBytes, LD.MimeType
                FROM [Loans].[LoanDocument] LD
                WHERE LD.Id = @Id AND LD.IsDeleted = 0";

            var document = await _connection.QueryFirstOrDefaultAsync<LoanDocumentResponse>(query, new { request.Id });
            return document ?? throw new NotFoundException($"Document {request.Id} not found");
        }
    }
}
