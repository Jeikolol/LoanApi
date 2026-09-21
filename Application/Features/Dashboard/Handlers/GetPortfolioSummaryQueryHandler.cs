using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    internal record PortfolioSummaryQueryResult(decimal TotalPortfolio, decimal DelinquencyRate, int ActiveCredits, decimal MonthlyCollections);

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

            // "Portfolio" is what is still owed on loans being repaid (not what was ever lent, nor loans not yet disbursed).
            const string sql = @"
                SELECT
                    ISNULL(SUM(PrincipalRemaining), 0.0) AS TotalPortfolio,
                    ISNULL(CAST(SUM(CASE WHEN DaysOverdue > 0 THEN PrincipalRemaining ELSE 0 END) * 100.0
                        / NULLIF(SUM(PrincipalRemaining), 0) AS DECIMAL(5,2)), 0.0) AS DelinquencyRate,
                    COUNT(*) AS ActiveCredits,
                    (SELECT ISNULL(SUM(TotalPaymentAmount), 0) FROM [Loans].[LoanPayment]
                     WHERE IsDeleted = 0 AND Status = 1
                       AND PaymentDate >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS MonthlyCollections
                FROM [Loans].[Loan]
                WHERE IsDeleted = 0 AND LoanStatus IN (2, 3, 4, 6, 8)";

            var result = await _connection.QuerySingleOrDefaultAsync<PortfolioSummaryQueryResult>(sql);

            if (result == null)
                return new PortfolioSummaryResponse(0, 0, 0, 0);

            return new PortfolioSummaryResponse(
                (long)result.TotalPortfolio,
                result.DelinquencyRate,
                result.ActiveCredits,
                (long)result.MonthlyCollections
            );
        }
    }
}
