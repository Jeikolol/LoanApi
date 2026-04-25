using Application.Constants;
using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
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

            var result = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql);

            return new[]
            {
                new PortfolioStatusItemResponse("Active", result?.Active ?? 0, PortfolioStatusColors.Active),
                new PortfolioStatusItemResponse("Delinquent", result?.Delinquent ?? 0, PortfolioStatusColors.Delinquent),
                new PortfolioStatusItemResponse("Approved", result?.Approved ?? 0, PortfolioStatusColors.Approved),
                new PortfolioStatusItemResponse("Written Off", result?.WrittenOff ?? 0, PortfolioStatusColors.WrittenOff)
            };
        }
    }
}
