using Application.Features.Loans.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class GetLoanDocumentsQueryHandler : IRequestHandler<GetLoanDocumentsQuery, PaginatedResponse<LoanDocumentResponse>>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetLoanDocumentsQueryHandler> _logger;

        public GetLoanDocumentsQueryHandler(IDbConnection connection, ILogger<GetLoanDocumentsQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<PaginatedResponse<LoanDocumentResponse>> Handle(GetLoanDocumentsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting documents for loan {LoanId} - Page: {PageNumber}, Size: {PageSize}",
                request.LoanId, request.PageNumber, request.PageSize);

            const string query = @"
                SELECT LD.Id, LD.LoanId, LD.DocumentType, LD.FileName, LD.FileUrl,
                       LD.UploadedDate, LD.FileSizeBytes, LD.MimeType
                FROM [Loans].[LoanDocument] LD
                WHERE LD.LoanId = @LoanId AND LD.IsDeleted = 0
                ORDER BY LD.UploadedDate DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                SELECT COUNT(*) FROM [Loans].[LoanDocument]
                WHERE LoanId = @LoanId AND IsDeleted = 0;";

            var args = new { request.LoanId, Skip = request.GetSkip(), Take = request.PageSize };

            using var multi = await _connection.QueryMultipleAsync(query, args);
            var documents = (await multi.ReadAsync<LoanDocumentResponse>()).ToList();
            var totalCount = await multi.ReadFirstAsync<int>();

            return new PaginatedResponse<LoanDocumentResponse>(documents, request.PageNumber, request.PageSize, totalCount);
        }
    }
}
