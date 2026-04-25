using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    public class GetDelinquencyTrendQueryHandler : IRequestHandler<GetDelinquencyTrendQuery, DelinquencyTrendResponse>
    {
        private readonly IDbConnection _connection;

        public GetDelinquencyTrendQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<DelinquencyTrendResponse> Handle(GetDelinquencyTrendQuery request, CancellationToken cancellationToken)
        {
            const string sql = @"
               WITH DateRange AS (
                  SELECT CAST(DATEADD(DAY, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), CAST(GETDATE() AS DATE)) AS DATE) AS [Date]
                  FROM (SELECT TOP 30 1 AS rn FROM sys.objects) a
                ),
                DailyAverage AS (
                  SELECT
                    d.[Date],
                    AVG(CAST(l.DaysOverdue AS DECIMAL)) AS DayAvg
                  FROM DateRange d
                  LEFT JOIN [Loans].[Loan] l ON CAST(l.UpdatedOn AS DATE) <= d.[Date] AND l.IsDeleted = 0
                  GROUP BY d.[Date]
                )
                SELECT
                  FORMAT([Date], 'dd/MM') AS Label,
                  ISNULL(CAST(AVG(DayAvg) OVER (ORDER BY [Date] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS DECIMAL(5,2)), 0.0) AS CurrentDaysOverdueAvg,
                  ISNULL(CAST(AVG(DayAvg) OVER (ORDER BY [Date] ROWS BETWEEN 30 PRECEDING AND 1 PRECEDING) AS DECIMAL(5,2)), 0.0) AS PreviousDaysOverdueAvg
                FROM DailyAverage
                ORDER BY [Date] DESC";

            var trends = (await _connection.QueryAsync<dynamic>(sql)).ToList();

            var labels = trends.Select(t => (string)t.Label).ToArray();
            var currentMonth = trends.Select(t => (decimal)(t.CurrentDaysOverdueAvg ?? 0m)).ToArray();
            var previousMonth = trends.Select(t => (decimal)(t.PreviousDaysOverdueAvg ?? 0m)).ToArray();

            return new DelinquencyTrendResponse(labels, currentMonth, previousMonth);
        }
    }
}
