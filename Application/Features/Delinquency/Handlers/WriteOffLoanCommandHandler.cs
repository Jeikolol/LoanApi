using Application.Exceptions;
using Application.Features.Delinquency.Commands;
using Application.Features.Loans.Commands;
using Application.Features.Payments;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency.Handlers
{
    public class WriteOffLoanCommandHandler : IRequestHandler<WriteOffLoanCommand, LoanTransitionResponse>
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<WriteOffLoanCommandHandler> _logger;

        public WriteOffLoanCommandHandler(IDbConnection connection, ILogger<WriteOffLoanCommandHandler> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<LoanTransitionResponse> Handle(WriteOffLoanCommand request, CancellationToken cancellationToken)
        {
            string reason = (request.Reason ?? string.Empty).Trim();
            if (reason.Length is < WriteOffRules.MinReasonLength or > WriteOffRules.MaxReasonLength)
            {
                throw new ValidationException(
                    "The write-off is not valid.",
                    [$"The reason is required: between {WriteOffRules.MinReasonLength} and {WriteOffRules.MaxReasonLength} characters."]);
            }

            LoanRow loan = await _connection.QuerySingleOrDefaultAsync<LoanRow>(
                @"SELECT LoanNumber, LoanStatus, PrincipalRemaining, NextPaymentDueDate
                  FROM [Loans].[Loan] WHERE Id = @LoanId AND IsDeleted = 0",
                new { request.LoanId })
                ?? throw new NotFoundException($"Loan {request.LoanId} not found");

            int daysUntilWriteOff = await _connection.QueryFirstOrDefaultAsync<int?>(
                @"SELECT DaysUntilWriteOff FROM [Loans].[DelinquencyPolicy]
                  WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CreatedOn DESC") ?? WriteOffRules.DefaultDaysUntilWriteOff;

            // Measured from the due date today, not from the stored counter, which is only as fresh as the last calculation.
            int daysOverdue = PaymentRules.DaysOverdue(loan.NextPaymentDueDate, DateOnly.FromDateTime(DateTime.UtcNow));

            string? problem = WriteOffRules.Problem(loan.LoanStatus, loan.PrincipalRemaining, daysOverdue, daysUntilWriteOff);
            if (problem is not null) throw new ConflictException(problem);

            // The status guard makes it atomic: a payment or another write-off that got there first leaves zero rows.
            int updated = await _connection.ExecuteAsync(
                @"UPDATE [Loans].[Loan]
                  SET LoanStatus = @WrittenOff, DaysOverdue = @DaysOverdue, UpdatedOn = @Now
                  WHERE Id = @LoanId AND IsDeleted = 0 AND LoanStatus = @Current",
                new
                {
                    request.LoanId,
                    WrittenOff = (int)LoanStatus.WrittenOff,
                    Current = (int)loan.LoanStatus,
                    DaysOverdue = daysOverdue,
                    Now = DateTime.UtcNow,
                });

            if (updated != 1)
            {
                throw new ConflictException("The loan changed while processing the request. Please refresh and try again.");
            }

            _logger.LogInformation("Loan {LoanId} written off after {Days} days overdue: {Reason}", request.LoanId, daysOverdue, reason);

            return new LoanTransitionResponse(request.LoanId, loan.LoanNumber, LoanStatus.WrittenOff);
        }

        private sealed class LoanRow
        {
            public string LoanNumber { get; set; } = string.Empty;
            public LoanStatus LoanStatus { get; set; }
            public decimal PrincipalRemaining { get; set; }
            public DateOnly NextPaymentDueDate { get; set; }
        }
    }
}
