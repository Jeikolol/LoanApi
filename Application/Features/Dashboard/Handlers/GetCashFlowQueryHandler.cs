using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    internal record CashFlowQueryResult(string WeekLabel, decimal Disbursements, decimal Collections);

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
                    CONCAT(FORMAT(DATEADD(DAY, -6, DATEADD(WEEK, -(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1), CAST(GETDATE() AS DATE))), 'dd/MM'), ' - ',
                           FORMAT(DATEADD(WEEK, -(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1), CAST(GETDATE() AS DATE)), 'dd/MM')) AS WeekLabel
                  FROM (SELECT TOP 4 1 AS rn FROM sys.objects) a
                )
                SELECT
                  w.WeekLabel,
                  ISNULL((SELECT SUM(l.PrincipalAmount)
                          FROM [Loans].[Loan] l
                          WHERE l.IsDeleted = 0 AND l.LoanStatus NOT IN (0, 1)
                            AND l.DisbursementDate >= DATEADD(DAY, -6, w.WeekDate)
                            AND l.DisbursementDate <  DATEADD(DAY, 1, w.WeekDate)), 0) AS Disbursements,
                  ISNULL((SELECT SUM(p.TotalPaymentAmount)
                          FROM [Loans].[LoanPayment] p
                          WHERE p.IsDeleted = 0 AND p.Status = 1
                            AND p.PaymentDate >= DATEADD(DAY, -6, w.WeekDate)
                            AND p.PaymentDate <  DATEADD(DAY, 1, w.WeekDate)), 0) AS Collections
                FROM Weeks w
                ORDER BY w.WeekNum DESC";

            var cashFlows = (await _connection.QueryAsync<CashFlowQueryResult>(sql)).ToList();

            var labels = cashFlows.Select(c => c.WeekLabel).ToArray();
            var disbursements = cashFlows.Select(c => c.Disbursements).ToArray();
            var collections = cashFlows.Select(c => c.Collections).ToArray();

            return new CashFlowResponse(labels, disbursements, collections);
        }
    }
}
