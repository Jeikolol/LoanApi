using Application.Features.CreditScores.Commands;
using Application.Models.Responses;
using Application.Services;
using Dapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.CreditScores.Handlers
{
    public class RecalculateCreditScoreCommandHandler : IRequestHandler<RecalculateCreditScoreCommand, CreditScoreResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<RecalculateCreditScoreCommandHandler> _logger;
        private readonly CreditScoreService _creditScoreService;

        public RecalculateCreditScoreCommandHandler(IDbConnection connection, ILogger<RecalculateCreditScoreCommandHandler> logger, CreditScoreService creditScoreService)
        {
            _connection = connection;
            _logger = logger;
            _creditScoreService = creditScoreService;
        }

        public async Task<CreditScoreResponse> Handle(RecalculateCreditScoreCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Recalculating credit score for customer {CustomerId}", request.CustomerId);

            const string customerSql = @"
                SELECT Id, FirstName, LastName, CreditLimit, IsBlacklisted
                FROM [Customers].[Customer]
                WHERE Id = @CustomerId AND IsDeleted = 0";

            var customerData = await _connection.QuerySingleOrDefaultAsync<dynamic>(
                customerSql, new { request.CustomerId });
            if (customerData == null)
                throw new InvalidOperationException($"Customer {request.CustomerId} not found");

            const string loansSql = @"
                SELECT Id, CreatedOn, PrincipalRemaining, LoanStatus
                FROM [Loans].[Loan]
                WHERE CustomerId = @CustomerId AND IsDeleted = 0";

            var loans = (await _connection.QueryAsync<dynamic>(
                loansSql, new { request.CustomerId })).ToList();

            const string paymentsSql = @"
                SELECT lp.Id, lp.LoanId, lp.Status
                FROM [Loans].[LoanPayment] lp
                WHERE lp.LoanId IN (SELECT Id FROM [Loans].[Loan] WHERE CustomerId = @CustomerId AND IsDeleted = 0)
                AND lp.IsDeleted = 0";

            var payments = (await _connection.QueryAsync<dynamic>(
                paymentsSql, new { request.CustomerId })).ToList();

            var customer = new Customer
            {
                Id = customerData.Id,
                FirstName = customerData.FirstName,
                LastName = customerData.LastName,
                CreditLimit = customerData.CreditLimit,
                IsBlacklisted = customerData.IsBlacklisted,
                Loans = loans.Select(l => new Loan
                {
                    Id = l.Id,
                    CreatedOn = l.CreatedOn,
                    PrincipalRemaining = l.PrincipalRemaining,
                    LoanStatus = (LoanStatus)l.LoanStatus,
                    Payments = payments
                        .Where(p => p.LoanId == l.Id)
                        .Select(p => new LoanPayment
                        {
                            Id = p.Id,
                            LoanId = p.LoanId,
                            Status = (PaymentStatus)p.Status
                        }).ToList()
                }).ToList()
            };

            var creditScore = _creditScoreService.CalculateCreditScore(customer);

            const string updateSql = @"
                UPDATE [Customers].[CreditScore]
                SET Score = @Score,
                    CreditRating = @CreditRating,
                    PaymentHistoryScore = @PaymentHistoryScore,
                    CreditUtilizationScore = @CreditUtilizationScore,
                    CreditAgeScore = @CreditAgeScore,
                    DefaultHistoryScore = @DefaultHistoryScore,
                    LastCalculatedDate = @LastCalculatedDate,
                    UpdatedOn = @Now
                WHERE CustomerId = @CustomerId AND IsDeleted = 0";

            await _connection.ExecuteAsync(updateSql, new
            {
                Score = creditScore.Score,
                CreditRating = (int)creditScore.CreditRating,
                PaymentHistoryScore = creditScore.PaymentHistoryScore,
                CreditUtilizationScore = creditScore.CreditUtilizationScore,
                CreditAgeScore = creditScore.CreditAgeScore,
                DefaultHistoryScore = creditScore.DefaultHistoryScore,
                LastCalculatedDate = creditScore.LastCalculatedDate,
                request.CustomerId,
                Now = DateTime.UtcNow
            });

            return new CreditScoreResponse(
                creditScore.Id,
                request.CustomerId,
                creditScore.Score,
                creditScore.CreditRating,
                creditScore.LastCalculatedDate,
                creditScore.PaymentHistoryScore,
                creditScore.CreditUtilizationScore,
                creditScore.CreditAgeScore,
                creditScore.DefaultHistoryScore
            );
        }
    }
}
