using Application.Features.Loans.Commands;
using Application.Models.Responses;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Loans.Handlers
{
    public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CreateLoanCommandHandler> _logger;

        public CreateLoanCommandHandler(IDbConnection connection, ILogger<CreateLoanCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public CreateLoanCommandHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<LoanDetailResponse> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new loan for customer {CustomerId}", request.CustomerId);

            var loanId = Guid.NewGuid();
            var today = DateTime.UtcNow;
            var maturityDate = today.AddMonths(request.TermMonths);
            var installmentAmount = request.PrincipalAmount / request.TermMonths;
            var loanNumber = $"LOAN-{loanId:N}".Substring(0, 12);

            const string insertQuery = @"
                INSERT INTO [Loans].[Loan] (
                    Id, LoanNumber, CustomerId, BranchId, CurrencyId, PrincipalAmount,
                    InterestRate, TermMonths, InstallmentAmount, LoanStatus,
                    DisbursementDate, MaturityDate, NextPaymentDueDate,
                    DaysOverdue, DelinquencyPercentage, PrincipalRemaining,
                    TotalInterestAmount, PaidInterest, RemainingInterest,
                    AmortizationBalance, PrincipalPaid, MaxCredit,
                    RequiresGuarantor, CreatedOn, IsActive, IsDeleted
                ) VALUES (
                    @Id, @LoanNumber, @CustomerId, @BranchId, @CurrencyId, @PrincipalAmount,
                    @InterestRate, @TermMonths, @InstallmentAmount, @LoanStatus,
                    @DisbursementDate, @MaturityDate, @NextPaymentDueDate,
                    @DaysOverdue, @DelinquencyPercentage, @PrincipalRemaining,
                    @TotalInterestAmount, @PaidInterest, @RemainingInterest,
                    @AmortizationBalance, @PrincipalPaid, @MaxCredit,
                    @RequiresGuarantor, @CreatedOn, @IsActive, @IsDeleted
                )";

            const string selectQuery = @"
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

            try
            {
                await _connection.ExecuteAsync(insertQuery, new
                {
                    Id = loanId,
                    LoanNumber = loanNumber,
                    request.CustomerId,
                    request.BranchId,
                    request.CurrencyId,
                    request.PrincipalAmount,
                    request.InterestRate,
                    request.TermMonths,
                    InstallmentAmount = installmentAmount,
                    LoanStatus = (int)request.LoanStatus,
                    DisbursementDate = today,
                    MaturityDate = maturityDate,
                    NextPaymentDueDate = today.AddMonths(1),
                    DaysOverdue = 0,
                    DelinquencyPercentage = 0m,
                    PrincipalRemaining = request.PrincipalAmount,
                    TotalInterestAmount = 0m,
                    PaidInterest = 0m,
                    RemainingInterest = 0m,
                    AmortizationBalance = request.PrincipalAmount,
                    PrincipalPaid = 0m,
                    MaxCredit = request.PrincipalAmount,
                    RequiresGuarantor = false,
                    CreatedOn = today,
                    IsActive = true,
                    IsDeleted = false
                });

                var loan = await _connection.QueryFirstOrDefaultAsync<LoanDetailResponse>(selectQuery, new { Id = loanId });
                return loan ?? throw new InvalidOperationException("Failed to retrieve created loan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating loan for customer {CustomerId}", request.CustomerId);
                throw new InvalidOperationException($"Failed to create loan for customer {request.CustomerId}", ex);
            }
        }
    }
}
