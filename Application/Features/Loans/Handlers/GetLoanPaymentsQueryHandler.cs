using Application.Features.Loans.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class GetLoanPaymentsQueryHandler : IRequestHandler<GetLoanPaymentsQuery, PaginatedResponse<LoanPaymentResponse>>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetLoanPaymentsQueryHandler> _logger;

        public GetLoanPaymentsQueryHandler(IDbConnection connection, ILogger<GetLoanPaymentsQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<PaginatedResponse<LoanPaymentResponse>> Handle(GetLoanPaymentsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting payments for loan {LoanId} - Page: {PageNumber}, Size: {PageSize}",
                request.LoanId, request.PageNumber, request.PageSize);

            const string query = @"
                SELECT LP.Id, LP.LoanId, LP.PaymentDate, LP.PrincipalAmount, LP.InterestAmount,
                       LP.PenaltyAmount, LP.TotalPaymentAmount, LP.PaymentMethod, LP.ReferenceNumber,
                       LP.Status, LP.CreatedOn
                FROM [Loans].[LoanPayment] LP
                WHERE LP.LoanId = @LoanId AND LP.IsDeleted = 0
                ORDER BY LP.PaymentDate DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

                SELECT COUNT(*) FROM [Loans].[LoanPayment]
                WHERE LoanId = @LoanId AND IsDeleted = 0;";

            var args = new { request.LoanId, Skip = request.GetSkip(), Take = request.PageSize };

            using var multi = await _connection.QueryMultipleAsync(query, args);
            var payments = (await multi.ReadAsync<LoanPaymentResponse>()).ToList();
            var totalCount = await multi.ReadFirstAsync<int>();

            return new PaginatedResponse<LoanPaymentResponse>(payments, request.PageNumber, request.PageSize, totalCount);
        }
    }
}
