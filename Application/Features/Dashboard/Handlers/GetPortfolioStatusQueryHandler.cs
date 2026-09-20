using Application.Constants;
using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    internal record PortfolioStatusQueryResult(int Active, int Delinquent, int Approved, int WrittenOff);

    public class GetPortfolioStatusQueryHandler : IRequestHandler<GetPortfolioStatusQuery, PortfolioStatusItemResponse[]>
    {
        private readonly IDbConnection _connection;

        public GetPortfolioStatusQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<PortfolioStatusItemResponse[]> Handle(GetPortfolioStatusQuery request, CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT
                  COUNT(CASE WHEN LoanStatus = 3 THEN 1 END) AS Active,
                  COUNT(CASE WHEN LoanStatus = 6 THEN 1 END) AS Delinquent,
                  COUNT(CASE WHEN LoanStatus = 1 THEN 1 END) AS Approved,
                  COUNT(CASE WHEN LoanStatus = 7 THEN 1 END) AS WrittenOff
                FROM [Loans].[Loan]
                WHERE IsDeleted = 0";

            var result = await _connection.QuerySingleOrDefaultAsync<PortfolioStatusQueryResult>(sql);

            if (result == null)
                return Array.Empty<PortfolioStatusItemResponse>();

            return new[]
            {
                new PortfolioStatusItemResponse("Active", result.Active, PortfolioStatusColors.Active),
                new PortfolioStatusItemResponse("Delinquent", result.Delinquent, PortfolioStatusColors.Delinquent),
                new PortfolioStatusItemResponse("Approved", result.Approved, PortfolioStatusColors.Approved),
                new PortfolioStatusItemResponse("Written Off", result.WrittenOff, PortfolioStatusColors.WrittenOff)
            };
        }
    }
}
