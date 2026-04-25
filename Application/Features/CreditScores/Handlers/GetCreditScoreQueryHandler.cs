using Application.Exceptions;
using Application.Features.CreditScores.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.CreditScores.Handlers
{
    public class GetCreditScoreQueryHandler : IRequestHandler<GetCreditScoreQuery, CreditScoreResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetCreditScoreQueryHandler> _logger;

        public GetCreditScoreQueryHandler(IDbConnection connection, ILogger<GetCreditScoreQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<CreditScoreResponse> Handle(GetCreditScoreQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting credit score for customer {CustomerId}", request.CustomerId);

            const string query = @"
                SELECT CS.Id, CS.CustomerId, CS.Score, CS.CreditRating, CS.LastCalculatedDate,
                       CS.PaymentHistoryScore, CS.CreditUtilizationScore,
                       CS.CreditAgeScore, CS.DefaultHistoryScore
                FROM [Customers].[CreditScore] CS
                WHERE CS.CustomerId = @CustomerId AND CS.IsDeleted = 0";

            var score = await _connection.QueryFirstOrDefaultAsync<CreditScoreResponse>(query, new { request.CustomerId });
            return score ?? throw new NotFoundException($"Credit score for customer {request.CustomerId} not found");
        }
    }
}
