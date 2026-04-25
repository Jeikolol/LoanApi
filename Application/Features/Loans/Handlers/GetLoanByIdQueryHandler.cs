using Application.Exceptions;
using Application.Features.Loans.Queries;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, LoanDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetLoanByIdQueryHandler> _logger;

        public GetLoanByIdQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public GetLoanByIdQueryHandler(IDbConnection connection, ILogger<GetLoanByIdQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDetailResponse> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting loan by ID: {LoanId}", request.Id);

            const string query = @"
                SELECT L.Id, L.LoanNumber, L.CustomerId, C.FirstName + ' ' + C.LastName AS CustomerName,
                       L.PrincipalAmount, L.InterestRate, L.TermMonths, L.InstallmentAmount,
                       L.LoanStatus, L.DisbursementDate, L.MaturityDate, L.NextPaymentDueDate,
                       L.DaysOverdue, L.DelinquencyPercentage, L.PrincipalRemaining,
                       L.BranchId, CUR.CodeIso3 AS CurrencyCode, L.LoanPurpose,
                       L.TotalInterestAmount, L.PaidInterest, L.RemainingInterest,
                       L.AmortizationBalance, L.PrincipalPaid, L.MaxCredit,
                       L.RequiresGuarantor, L.GuarantorId, L.LastPaymentDate,
                       L.CreatedOn, L.UpdatedOn
                FROM [Loans].[Loan] L
                INNER JOIN [Customers].[Customer] C ON L.CustomerId = C.Id
                INNER JOIN [Reference].[Currency] CUR ON L.CurrencyId = CUR.Id
                WHERE L.Id = @Id AND L.IsDeleted = 0";

            var loan = await _connection.QueryFirstOrDefaultAsync<LoanDetailResponse>(query, new { request.Id });
            return loan ?? throw new NotFoundException($"Loan {request.Id} not found");
        }
    }
}
