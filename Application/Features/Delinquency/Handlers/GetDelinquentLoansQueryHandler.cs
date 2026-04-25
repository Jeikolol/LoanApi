using Application.Features.Delinquency.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency.Handlers
{
    public class GetDelinquentLoansQueryHandler : IRequestHandler<GetDelinquentLoansQuery, PaginatedResponse<LoanSummaryResponse>>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetDelinquentLoansQueryHandler> _logger;

        public GetDelinquentLoansQueryHandler(IDbConnection connection, ILogger<GetDelinquentLoansQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<PaginatedResponse<LoanSummaryResponse>> Handle(GetDelinquentLoansQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting delinquent loans - Page: {PageNumber}", request.PageNumber);

            const string query = @"
                SELECT L.Id, L.LoanNumber, L.CustomerId, C.FirstName + ' ' + C.LastName AS CustomerName,
                       L.PrincipalAmount, L.InterestRate, L.TermMonths, L.InstallmentAmount,
                       L.LoanStatus, L.DisbursementDate, L.MaturityDate, L.NextPaymentDueDate,
                       L.DaysOverdue, L.DelinquencyPercentage, L.PrincipalRemaining
                FROM [Loans].[Loan] L
                INNER JOIN [Customers].[Customer] C ON L.CustomerId = C.Id
                WHERE L.DaysOverdue > 0 AND L.IsDeleted = 0
                ORDER BY L.DaysOverdue DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                SELECT COUNT(*) FROM [Loans].[Loan]
                WHERE DaysOverdue > 0 AND IsDeleted = 0;";

            var args = new { Skip = request.GetSkip(), Take = request.PageSize };

            using var multi = await _connection.QueryMultipleAsync(query, args);
            var loans = (await multi.ReadAsync<LoanSummaryResponse>()).ToList();
            var totalCount = await multi.ReadFirstAsync<int>();

            return new PaginatedResponse<LoanSummaryResponse>(loans, request.PageNumber, request.PageSize, totalCount);
        }
    }
}
