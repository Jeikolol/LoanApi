using Application.Exceptions;
using Application.Features.Loans.Queries;
using Application.Services;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    internal record AmortizationLoanQueryResult(decimal PrincipalAmount, decimal InterestRate, int TermMonths, DateTime DisbursementDate);

    public class GetAmortizationScheduleQueryHandler : IRequestHandler<GetAmortizationScheduleQuery, List<AmortizationScheduleItem>>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<GetAmortizationScheduleQueryHandler> _logger;

        public GetAmortizationScheduleQueryHandler(
            IDbConnection connection,
            ILogger<GetAmortizationScheduleQueryHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<List<AmortizationScheduleItem>> Handle(GetAmortizationScheduleQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting amortization schedule for loan {LoanId}", request.LoanId);

            const string sql = @"
                SELECT PrincipalAmount, InterestRate, TermMonths, DisbursementDate
                FROM [Loans].[Loan]
                WHERE Id = @LoanId AND IsDeleted = 0";

            var loan = await _connection.QuerySingleOrDefaultAsync<AmortizationLoanQueryResult>(sql, new { request.LoanId });
            if (loan == null)
                throw new NotFoundException($"Loan {request.LoanId} not found");

            // The same annuity the loan's installment was computed with, so the schedule always agrees with it.
            var installments = AmortizationSchedule.Build(loan.PrincipalAmount, loan.InterestRate, loan.TermMonths);
            var schedule = installments.Select(i => new AmortizationScheduleItem
            {
                PaymentNumber = i.Number,
                DueDate = loan.DisbursementDate.AddMonths(i.Number),
                PrincipalPayment = i.Principal,
                InterestPayment = i.Interest,
                TotalPayment = i.Payment,
                RemainingBalance = i.BalanceAfter,
            }).ToList();

            return schedule;
        }
    }
}
