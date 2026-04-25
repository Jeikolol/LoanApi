using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    public class GetBranchPortfolioQueryHandler : IRequestHandler<GetBranchPortfolioQuery, BranchPortfolioItemResponse[]>
    {
        private readonly IDbConnection _connection;

        public GetBranchPortfolioQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<BranchPortfolioItemResponse[]> Handle(GetBranchPortfolioQuery request, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT
                  b.Name AS Branch,
                  SUM(l.PrincipalRemaining) AS Portfolio,
                  CAST(SUM(CASE WHEN l.DaysOverdue > 0 THEN l.PrincipalRemaining ELSE 0 END) * 100.0
                       / NULLIF(SUM(l.PrincipalRemaining), 0) AS DECIMAL(5,2)) AS DelinquencyRate,
                  COUNT(CASE WHEN l.DaysOverdue > 30 THEN 1 END) AS OpenTickets
                FROM [Loans].[Loan] l
                JOIN [Admin].[Branch] b ON l.BranchId = b.Id
                WHERE l.IsDeleted = 0
                GROUP BY b.Id, b.Name
                ORDER BY Portfolio DESC";

            var branches = (await _connection.QueryAsync<dynamic>(sql)).ToList();

            return branches.Select(b => new BranchPortfolioItemResponse(
                (string)b.Branch,
                (long)(b.Portfolio ?? 0L),
                (decimal)(b.DelinquencyRate ?? 0m),
                (int)(b.OpenTickets ?? 0)
            )).ToArray();
        }
    }
}
