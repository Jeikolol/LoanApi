using Application.Exceptions;
using Application.Features.Payments.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Payments.Handlers
{
    public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, LoanPaymentResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetPaymentByIdQueryHandler> _logger;

        public GetPaymentByIdQueryHandler(IDbConnection connection, ILogger<GetPaymentByIdQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanPaymentResponse> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting payment by ID: {PaymentId}", request.Id);

            const string query = @"
                SELECT LP.Id, LP.LoanId, LP.PaymentDate, LP.PrincipalAmount, LP.InterestAmount,
                       LP.PenaltyAmount, LP.TotalPaymentAmount, LP.PaymentMethod, LP.ReferenceNumber,
                       LP.Status, LP.CreatedOn
                FROM [Loans].[LoanPayment] LP
                WHERE LP.Id = @Id AND LP.IsDeleted = 0";

            var payment = await _connection.QueryFirstOrDefaultAsync<LoanPaymentResponse>(query, new { request.Id });
            return payment ?? throw new NotFoundException($"Payment {request.Id} not found");
        }
    }
}
