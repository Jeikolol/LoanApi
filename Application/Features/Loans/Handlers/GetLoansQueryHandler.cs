using Application.Features.Loans.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class GetLoansQueryHandler : IRequestHandler<GetLoansQuery, PaginatedResponse<LoanSummaryResponse>>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetLoansQueryHandler> _logger;

        public GetLoansQueryHandler(IDbConnection connection, ILogger<GetLoansQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<PaginatedResponse<LoanSummaryResponse>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting loans with pagination - Page: {PageNumber}, Size: {PageSize}", request.PageNumber, request.PageSize);

            const string query = @"
                SELECT L.Id, L.LoanNumber, L.CustomerId, C.FirstName + ' ' + C.LastName AS CustomerName,
                       L.PrincipalAmount, L.InterestRate, L.TermMonths, L.InstallmentAmount,
                       L.LoanStatus, L.DisbursementDate, L.MaturityDate, L.NextPaymentDueDate,
                       L.DaysOverdue, L.DelinquencyPercentage, L.PrincipalRemaining
                FROM [Loans].[Loan] L
                INNER JOIN [Customers].[Customer] C ON L.CustomerId = C.Id
                WHERE L.IsDeleted = 0
                ORDER BY L.CreatedOn DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                SELECT COUNT(*) FROM [Loans].[Loan] WHERE IsDeleted = 0;";

            var args = new { Skip = request.GetSkip(), Take = request.PageSize };

            using var multi = await _connection.QueryMultipleAsync(query, args);
            var loans = (await multi.ReadAsync<LoanSummaryResponse>()).ToList();
            var totalCount = await multi.ReadFirstAsync<int>();

            return new PaginatedResponse<LoanSummaryResponse>(loans, request.PageNumber, request.PageSize, totalCount);
        }
    }
}
