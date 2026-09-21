using Application.Exceptions;
using Application.Features.Loans.Commands;
using Application.Interfaces;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class TransitionLoanCommandHandler : IRequestHandler<TransitionLoanCommand, LoanTransitionResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<TransitionLoanCommandHandler> _logger;

        public TransitionLoanCommandHandler(IDbConnection connection, ICurrentUser currentUser, ILogger<TransitionLoanCommandHandler> logger)
        {
            _connection = connection;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<LoanTransitionResponse> Handle(TransitionLoanCommand request, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(request.Action))
            {
                throw new ValidationException("The action is not valid.", ["Action is not valid."]);
            }

            LoanRow loan = await _connection.QuerySingleOrDefaultAsync<LoanRow>(
                @"SELECT LoanNumber, LoanStatus, CustomerId, PrincipalAmount, TermMonths, CreatedById
                  FROM [Loans].[Loan] WHERE Id = @LoanId AND IsDeleted = 0",
                new { request.LoanId })
                ?? throw new NotFoundException($"Loan {request.LoanId} not found");

            LoanStatus next = LoanLifecycleRules.Apply(loan.LoanStatus, request.Action)
                ?? throw new ConflictException($"A loan with status {loan.LoanStatus} cannot be {Describe(request.Action)}.");

            if (request.Action == LoanAction.Approve)
            {
                string? problem = LoanLifecycleRules.ApprovalProblem(loan.CreatedById, _currentUser.UserId);
                if (problem is not null) throw new ConflictException(problem);

                await EnsureEligible(request.LoanId, loan);
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            bool disburse = request.Action == LoanAction.Disburse;

            // The status guard makes the transition atomic: of two concurrent requests only one finds the loan
            // still in the status it was validated against.
            int updated = await _connection.ExecuteAsync(
                @"UPDATE [Loans].[Loan]
                  SET LoanStatus = @Next,
                      DisbursementDate = CASE WHEN @Disburse = 1 THEN @Today ELSE DisbursementDate END,
                      MaturityDate = CASE WHEN @Disburse = 1 THEN @Maturity ELSE MaturityDate END,
                      NextPaymentDueDate = CASE WHEN @Disburse = 1 THEN @FirstDue ELSE NextPaymentDueDate END,
                      UpdatedOn = @Now
                  WHERE Id = @LoanId AND IsDeleted = 0 AND LoanStatus = @Current",
                new
                {
                    request.LoanId,
                    Next = (int)next,
                    Current = (int)loan.LoanStatus,
                    Disburse = disburse ? 1 : 0,
                    Today = today,
                    Maturity = today.AddMonths(loan.TermMonths),
                    FirstDue = today.AddMonths(1),
                    Now = DateTime.UtcNow,
                });

            if (updated != 1)
            {
                throw new ConflictException("The loan changed while processing the request. Please refresh and try again.");
            }

            _logger.LogInformation("Loan {LoanId} moved from {From} to {To}", request.LoanId, loan.LoanStatus, next);

            return new LoanTransitionResponse(request.LoanId, loan.LoanNumber, next);
        }

        private async Task EnsureEligible(Guid loanId, LoanRow loan)
        {
            CustomerRow customer = await _connection.QuerySingleOrDefaultAsync<CustomerRow>(
                "SELECT IsBlacklisted, Status, CreditLimit FROM [Customers].[Customer] WHERE Id = @Id AND IsDeleted = 0",
                new { Id = loan.CustomerId })
                ?? throw new NotFoundException($"Customer {loan.CustomerId} not found");

            ExposureRow exposure = await _connection.QuerySingleAsync<ExposureRow>(
                @"SELECT COALESCE(SUM(PrincipalRemaining), 0) AS OpenExposure, COALESCE(MAX(DaysOverdue), 0) AS MaxDaysOverdue
                  FROM [Loans].[Loan]
                  WHERE CustomerId = @CustomerId AND Id <> @LoanId AND IsDeleted = 0 AND LoanStatus IN @Statuses",
                new
                {
                    loan.CustomerId,
                    LoanId = loanId,
                    Statuses = LoanLifecycleRules.StatusesCountedAsExposure.Select(s => (int)s).ToArray(),
                });

            List<string> problems = LoanLifecycleRules.EligibilityProblems(
                customer.IsBlacklisted, customer.Status, customer.CreditLimit,
                exposure.OpenExposure, exposure.MaxDaysOverdue, loan.PrincipalAmount);

            if (problems.Count > 0)
            {
                throw new ConflictException(string.Join(" ", problems));
            }
        }

        private static string Describe(LoanAction action) => action switch
        {
            LoanAction.Approve => "approved",
            LoanAction.Disburse => "disbursed",
            LoanAction.Cancel => "cancelled",
            _ => "closed",
        };

        private sealed class LoanRow
        {
            public string LoanNumber { get; set; } = string.Empty;
            public LoanStatus LoanStatus { get; set; }
            public Guid CustomerId { get; set; }
            public decimal PrincipalAmount { get; set; }
            public int TermMonths { get; set; }
            public Guid? CreatedById { get; set; }
        }

        private sealed class CustomerRow
        {
            public bool IsBlacklisted { get; set; }
            public CustomerStatus Status { get; set; }
            public decimal CreditLimit { get; set; }
        }

        private sealed class ExposureRow
        {
            public decimal OpenExposure { get; set; }
            public int MaxDaysOverdue { get; set; }
        }
    }
}
