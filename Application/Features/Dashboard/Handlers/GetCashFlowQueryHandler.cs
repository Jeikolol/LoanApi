using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    public class GetCashFlowQueryHandler : IRequestHandler<GetCashFlowQuery, CashFlowResponse>
    {
        private readonly IDbConnection _connection;

        public GetCashFlowQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<CashFlowResponse> Handle(GetCashFlowQuery request, CancellationToken cancellationToken)
        {
            const string sql = @"
                WITH Weeks AS (
                  SELECT
                    ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS WeekNum,
                    DATEADD(WEEK, -(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1), CAST(GETDATE() AS DATE)) AS WeekDate,
                    CONCAT('Week ', ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS WeekLabel
                  FROM (SELECT TOP 4 1 AS rn FROM sys.objects) a
                )
                SELECT
                  w.WeekLabel,
                  ISNULL(SUM(CASE WHEN CAST(l.DisbursementDate AS DATE) BETWEEN DATEADD(DAY, -7, w.WeekDate) AND w.WeekDate
                                     THEN l.PrincipalAmount ELSE 0 END), 0) AS Disbursements,
                  ISNULL(SUM(CASE WHEN CAST(p.PaymentDate AS DATE) BETWEEN DATEADD(DAY, -7, w.WeekDate) AND w.WeekDate
                                     THEN p.TotalPaymentAmount ELSE 0 END), 0) AS Collections
                FROM Weeks w
                LEFT JOIN [Loans].[Loan] l ON l.IsDeleted = 0
                LEFT JOIN [Loans].[LoanPayment] p ON p.IsDeleted = 0
                GROUP BY w.WeekNum, w.WeekLabel, w.WeekDate
                ORDER BY w.WeekNum";

            var cashFlows = (await _connection.QueryAsync<dynamic>(sql)).ToList();

            var labels = cashFlows.Select(c => (string)c.WeekLabel).ToArray();
            var disbursements = cashFlows.Select(c => (decimal)(c.Disbursements ?? 0m)).ToArray();
            var collections = cashFlows.Select(c => (decimal)(c.Collections ?? 0m)).ToArray();

            return new CashFlowResponse(labels, disbursements, collections);
        }
    }
}
