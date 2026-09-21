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
    public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, LoanDetailResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CreateLoanCommandHandler> _logger;

        public CreateLoanCommandHandler(IDbConnection connection, ILogger<CreateLoanCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanDetailResponse> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
        {
            Validate(request);
            await EnsureCustomerAndBranch(request);

            _logger.LogInformation("Creating new loan for customer {CustomerId}", request.CustomerId);

            var loanId = Guid.NewGuid();
            var today = DateTime.UtcNow;
            var maturityDate = today.AddMonths(request.TermMonths);
            var installmentAmount = AmortizationSchedule.Installment(request.PrincipalAmount, request.InterestRate, request.TermMonths);
            var totalInterest = AmortizationSchedule.TotalInterest(request.PrincipalAmount, request.InterestRate, request.TermMonths);
            var loanNumber = $"LOAN-{loanId:N}".Substring(0, 12);

            // Callers that do not pick a currency get the Dominican peso.
            var currencyId = request.CurrencyId != Guid.Empty
                ? request.CurrencyId
                : await _connection.QueryFirstOrDefaultAsync<Guid?>("SELECT Id FROM [Reference].[Currency] WHERE CodeIso3 = 'DOP'")
                    ?? throw new ValidationException("The loan request is not valid.", ["The default currency (DOP) does not exist."]);

            const string insertQuery = @"
                INSERT INTO [Loans].[Loan] (
                    Id, LoanNumber, CustomerId, BranchId, CurrencyId, PrincipalAmount,
                    InterestRate, TermMonths, InstallmentAmount, LoanStatus,
                    DisbursementDate, MaturityDate, NextPaymentDueDate,
                    DaysOverdue, DelinquencyPercentage, PrincipalRemaining,
                    TotalInterestAmount, PaidInterest, RemainingInterest,
                    AmortizationBalance, PrincipalPaid, MaxCredit,
                    RequiresGuarantor, CreatedOn, CreatedById, IsActive, IsDeleted
                ) VALUES (
                    @Id, @LoanNumber, @CustomerId, @BranchId, @CurrencyId, @PrincipalAmount,
                    @InterestRate, @TermMonths, @InstallmentAmount, @LoanStatus,
                    @DisbursementDate, @MaturityDate, @NextPaymentDueDate,
                    @DaysOverdue, @DelinquencyPercentage, @PrincipalRemaining,
                    @TotalInterestAmount, @PaidInterest, @RemainingInterest,
                    @AmortizationBalance, @PrincipalPaid, @MaxCredit,
                    @RequiresGuarantor, @CreatedOn, @CreatedById, @IsActive, @IsDeleted
                )";

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

            try
            {
                await _connection.ExecuteAsync(insertQuery, new
                {
                    Id = loanId,
                    LoanNumber = loanNumber,
                    request.CustomerId,
                    request.BranchId,
                    CurrencyId = currencyId,
                    request.PrincipalAmount,
                    request.InterestRate,
                    request.TermMonths,
                    InstallmentAmount = installmentAmount,
                    LoanStatus = (int)LoanStatus.Pending,
                    DisbursementDate = today,
                    MaturityDate = maturityDate,
                    NextPaymentDueDate = today.AddMonths(1),
                    DaysOverdue = 0,
                    DelinquencyPercentage = 0m,
                    PrincipalRemaining = request.PrincipalAmount,
                    TotalInterestAmount = totalInterest,
                    PaidInterest = 0m,
                    RemainingInterest = totalInterest,
                    AmortizationBalance = request.PrincipalAmount,
                    PrincipalPaid = 0m,
                    MaxCredit = request.PrincipalAmount,
                    RequiresGuarantor = false,
                    CreatedOn = today,
                    request.CreatedById,
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

        /// <summary>
        /// A soft-deleted customer would still satisfy the foreign key, and a blacklisted or non-active one can
        /// never be approved, so both are refused up front instead of leaving a pointless pending loan.
        /// </summary>
        private async Task EnsureCustomerAndBranch(CreateLoanCommand request)
        {
            CustomerCheck? customer = await _connection.QueryFirstOrDefaultAsync<CustomerCheck>(
                "SELECT Status, IsBlacklisted FROM [Customers].[Customer] WHERE Id = @Id AND IsDeleted = 0", new { Id = request.CustomerId });
            if (customer is null) throw new NotFoundException($"Customer {request.CustomerId} not found");

            if (customer.IsBlacklisted) throw new ConflictException("The customer is blacklisted and cannot receive a loan.");
            if (customer.Status != CustomerStatus.Active) throw new ConflictException($"The customer is {customer.Status}, not Active.");

            int branches = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM [Admin].[Branch] WHERE Id = @Id AND IsDeleted = 0", new { Id = request.BranchId });
            if (branches == 0) throw new ValidationException("The loan request is not valid.", ["The branch does not exist."]);
        }

        private sealed class CustomerCheck
        {
            public CustomerStatus Status { get; set; }
            public bool IsBlacklisted { get; set; }
        }

        private const decimal MaxPrincipal = 100_000_000m;
        private const decimal MaxAnnualRate = 100m;
        private const int MaxTermMonths = 360;

        private static void Validate(CreateLoanCommand request)
        {
            List<string> errors = [];

            if (request.CustomerId == Guid.Empty) errors.Add("CustomerId is required.");
            if (request.BranchId == Guid.Empty) errors.Add("BranchId is required.");
            if (request.PrincipalAmount <= 0) errors.Add("PrincipalAmount must be greater than zero.");
            if (request.PrincipalAmount > MaxPrincipal) errors.Add($"PrincipalAmount cannot exceed {MaxPrincipal:N0}.");
            if (request.PrincipalAmount > 0 && decimal.Round(request.PrincipalAmount, 2) != request.PrincipalAmount) errors.Add("PrincipalAmount cannot have more than two decimals.");
            if (request.InterestRate < 0) errors.Add("InterestRate cannot be negative.");
            if (request.InterestRate > MaxAnnualRate) errors.Add($"InterestRate cannot exceed {MaxAnnualRate}%.");
            if (decimal.Round(request.InterestRate, 2) != request.InterestRate) errors.Add("InterestRate cannot have more than two decimals.");
            if (request.TermMonths <= 0) errors.Add("TermMonths must be greater than zero.");
            if (request.TermMonths > MaxTermMonths) errors.Add($"TermMonths cannot exceed {MaxTermMonths}.");

            if (errors.Count > 0)
            {
                throw new ValidationException("The loan request is not valid.", errors);
            }
        }
    }
}
