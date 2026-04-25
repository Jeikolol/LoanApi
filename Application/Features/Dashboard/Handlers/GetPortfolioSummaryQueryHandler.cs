using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    public class GetPortfolioSummaryQueryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetPortfolioSummaryQueryHandler> _logger;

        public GetPortfolioSummaryQueryHandler(IDbConnection connection, ILogger<GetPortfolioSummaryQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<PortfolioSummaryResponse> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting portfolio summary");

            const string sql = @"
                SELECT
                    ISNULL(SUM(PrincipalAmount), 0.0) AS TotalPortfolio,
                    ISNULL(CAST(SUM(CASE WHEN DaysOverdue > 0 THEN PrincipalRemaining ELSE 0 END) * 100.0
                        / NULLIF(SUM(PrincipalRemaining), 0) AS DECIMAL(5,2)), 0.0) AS DelinquencyRate,
                    COUNT(CASE WHEN LoanStatus = 3 THEN 1 END) AS ActiveCredits,
                    (SELECT ISNULL(SUM(TotalPaymentAmount), 0) FROM [Loans].[LoanPayment]
                    WHERE IsDeleted = 0 AND CAST(PaymentDate AS DATE) = CAST(GETDATE() AS DATE)) AS MonthlyCollections
                FROM [Loans].[Loan]
                WHERE IsDeleted = 0";

            var result = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql);

            return new PortfolioSummaryResponse(
                (long)(result?.TotalPortfolio ?? 0m),
                (decimal)(result?.DelinquencyRate ?? 0m),
                (int)(result?.ActiveCredits ?? 0),
                (long)(result?.MonthlyCollections ?? 0m)
            );
        }
    }
}
