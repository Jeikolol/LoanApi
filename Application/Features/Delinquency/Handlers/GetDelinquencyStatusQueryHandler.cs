using Application.Features.Delinquency.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency.Handlers
{
    public class GetDelinquencyStatusQueryHandler : IRequestHandler<GetDelinquencyStatusQuery, DelinquencyStatusResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetDelinquencyStatusQueryHandler> _logger;

        public GetDelinquencyStatusQueryHandler(IDbConnection connection, ILogger<GetDelinquencyStatusQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<DelinquencyStatusResponse> Handle(GetDelinquencyStatusQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting delinquency status for loan {LoanId}", request.LoanId);

            const string query = @"
                SELECT L.Id AS LoanId, L.LoanNumber, L.DaysOverdue, L.DelinquencyPercentage, L.LoanStatus
                FROM [Loans].[Loan] L
                WHERE L.Id = @LoanId AND L.IsDeleted = 0";

            var status = await _connection.QueryFirstOrDefaultAsync<DelinquencyStatusResponse>(query, new { request.LoanId });
            return status ?? throw new Exception($"Loan {request.LoanId} not found");
        }
    }
}
