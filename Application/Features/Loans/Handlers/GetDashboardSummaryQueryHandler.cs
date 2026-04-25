using Application.Features.Loans.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, LoanDashboardSummaryResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetDashboardSummaryQueryHandler> _logger;

        public GetDashboardSummaryQueryHandler(IDbConnection connection, ILogger<GetDashboardSummaryQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDashboardSummaryResponse> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting dashboard summary");

            const string sql = @"
                SELECT
                  COUNT(*) AS TotalLoans,
                  SUM(CASE WHEN LoanStatus = 3 THEN 1 ELSE 0 END) AS TotalActiveLoans,
                  SUM(PrincipalAmount) AS TotalPrincipalDisbursed,
                  SUM(CASE WHEN DaysOverdue > 0 THEN 1 ELSE 0 END) AS TotalDelinquentLoans,
                  SUM(PrincipalRemaining) AS TotalPrincipalRemaining
                FROM [Loans].[Loan] WHERE IsDeleted = 0";

            var result = await _connection.QuerySingleOrDefaultAsync<LoanDashboardSummaryResponse>(sql);
            return result ?? new LoanDashboardSummaryResponse(0, 0, 0, 0, 0);
        }
    }
}
