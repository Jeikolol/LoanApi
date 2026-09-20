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
        private readonly LoanValidationService _loanValidationService;

        public GetAmortizationScheduleQueryHandler(
            IDbConnection connection,
            ILogger<GetAmortizationScheduleQueryHandler> logger,
            LoanValidationService loanValidationService)
        {
            _connection = connection;
            _logger = logger;
            _loanValidationService = loanValidationService;
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
                throw new InvalidOperationException($"Loan {request.LoanId} not found");

            var schedule = _loanValidationService.GenerateAmortizationSchedule(
                loan.PrincipalAmount,
                loan.InterestRate,
                loan.TermMonths);

            for (int i = 0; i < schedule.Count; i++)
            {
                schedule[i].DueDate = loan.DisbursementDate.AddMonths(i + 1);
                schedule[i].TotalPayment = schedule[i].PrincipalPayment + schedule[i].InterestPayment;
            }

            return schedule;
        }
    }
}
