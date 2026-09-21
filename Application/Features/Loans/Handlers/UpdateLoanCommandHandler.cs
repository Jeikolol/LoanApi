using Application.Exceptions;
using Application.Features.Loans.Commands;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class UpdateLoanCommandHandler : IRequestHandler<UpdateLoanCommand, LoanDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<UpdateLoanCommandHandler> _logger;

        public UpdateLoanCommandHandler(IDbConnection connection, ILogger<UpdateLoanCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDetailResponse> Handle(UpdateLoanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating loan {LoanId}", request.Id);

            if (request.LoanStatus is not null)
            {
                throw new ValidationException(
                    "The loan is not valid.",
                    ["The status cannot be set directly; use approve, disburse, cancel or close."]);
            }

            if (request.InterestRate is < 0)
            {
                throw new ValidationException("The loan is not valid.", ["InterestRate cannot be negative."]);
            }

            LoanTerms loan = await _connection.QuerySingleOrDefaultAsync<LoanTerms>(
                "SELECT LoanStatus, PrincipalAmount, InterestRate, TermMonths FROM [Loans].[Loan] WHERE Id = @Id AND IsDeleted = 0",
                new { request.Id })
                ?? throw new NotFoundException($"Loan {request.Id} not found");

            if (request.InterestRate is not null && request.InterestRate != loan.InterestRate)
            {
                // The installment, the schedule and every payment are built on the rate, so it is only
                // negotiable until the loan is approved.
                if (loan.LoanStatus != LoanStatus.Pending)
                {
                    throw new ConflictException($"The interest rate of a loan with status {loan.LoanStatus} cannot be changed.");
                }

                decimal rate = request.InterestRate.Value;
                decimal installment = AmortizationSchedule.Installment(loan.PrincipalAmount, rate, loan.TermMonths);
                decimal totalInterest = AmortizationSchedule.TotalInterest(loan.PrincipalAmount, rate, loan.TermMonths);

                await _connection.ExecuteAsync(
                    @"UPDATE [Loans].[Loan]
                      SET InterestRate = @Rate, InstallmentAmount = @Installment,
                          TotalInterestAmount = @TotalInterest, RemainingInterest = @TotalInterest, UpdatedOn = @Now
                      WHERE Id = @Id AND IsDeleted = 0 AND LoanStatus = @Pending",
                    new
                    {
                        request.Id,
                        Rate = rate,
                        Installment = installment,
                        TotalInterest = totalInterest,
                        Now = DateTime.UtcNow,
                        Pending = (int)LoanStatus.Pending,
                    });
            }

            const string selectQuery = @"
                SELECT L.Id, L.LoanNumber, L.CustomerId, C.FirstName + ' ' + C.LastName AS CustomerName,
                       L.PrincipalAmount, L.InterestRate, L.TermMonths, L.InstallmentAmount,
                       L.LoanStatus, L.DisbursementDate, L.MaturityDate, L.NextPaymentDueDate,
                       L.DaysOverdue, L.DelinquencyPercentage, L.PrincipalRemaining,
                       L.BranchId, CUR.CodeIso3 AS CurrencyCode, L.LoanPurpose,
                       L.TotalInterestAmount, L.PaidInterest, L.RemainingInterest,
                       L.LateFeesCharged,
                       (SELECT ISNULL(SUM(P.PenaltyAmount), 0) FROM [Loans].[LoanPayment] P WHERE P.LoanId = L.Id AND P.Status = 1 AND P.IsDeleted = 0) AS LateFeesPaid,
                       L.AmortizationBalance, L.PrincipalPaid, L.MaxCredit,
                       L.RequiresGuarantor, L.GuarantorId, L.LastPaymentDate,
                       L.CreatedOn, L.UpdatedOn
                FROM [Loans].[Loan] L
                INNER JOIN [Customers].[Customer] C ON L.CustomerId = C.Id
                INNER JOIN [Reference].[Currency] CUR ON L.CurrencyId = CUR.Id
                WHERE L.Id = @Id AND L.IsDeleted = 0";

            LoanDetailResponse? updated = await _connection.QueryFirstOrDefaultAsync<LoanDetailResponse>(selectQuery, new { request.Id });
            return updated ?? throw new NotFoundException($"Loan {request.Id} not found");
        }

        private sealed class LoanTerms
        {
            public LoanStatus LoanStatus { get; set; }
            public decimal PrincipalAmount { get; set; }
            public decimal InterestRate { get; set; }
            public int TermMonths { get; set; }
        }
    }
}
