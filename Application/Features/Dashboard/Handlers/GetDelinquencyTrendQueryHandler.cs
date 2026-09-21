using Application.Features.Dashboard.Queries;
using Application.Models.Responses.Dashboard;
using Dapper;
using Domain.Enums;
using MediatR;
using System.Data;

namespace Application.Features.Dashboard.Handlers
{
    public class GetDelinquencyTrendQueryHandler : IRequestHandler<GetDelinquencyTrendQuery, DelinquencyTrendResponse>
    {
        public const int Days = 30;

        private readonly IDbConnection _connection;

        public GetDelinquencyTrendQueryHandler(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<DelinquencyTrendResponse> Handle(GetDelinquencyTrendQuery request, CancellationToken cancellationToken)
        {
            // Loans that were ever disbursed (a pending, approved or cancelled loan has no schedule to be late on).
            List<HistoryLoan> loans = [.. (await _connection.QueryAsync<LoanRow>(
                @"SELECT Id, DisbursementDate, MaturityDate, TermMonths, InstallmentAmount
                  FROM [Loans].[Loan]
                  WHERE IsDeleted = 0 AND LoanStatus NOT IN @Excluded",
                new { Excluded = new[] { (int)LoanStatus.Pending, (int)LoanStatus.Approved } }))
                .Select(l => new HistoryLoan(l.Id, l.DisbursementDate, l.MaturityDate, l.TermMonths, l.InstallmentAmount))];

            List<HistoryPayment> payments = [.. (await _connection.QueryAsync<PaymentRow>(
                @"SELECT LoanId, PaymentDate, PrincipalAmount + InterestAmount AS Amount, ReversedOn, Status
                  FROM [Loans].[LoanPayment]
                  WHERE IsDeleted = 0 AND Status IN @Statuses",
                new { Statuses = new[] { (int)PaymentStatus.Completed, (int)PaymentStatus.Reversed } }))
                .Select(p => new HistoryPayment(
                    p.LoanId,
                    DateOnly.FromDateTime(p.PaymentDate),
                    p.Amount,
                    p.Status == PaymentStatus.Reversed ? DateOnly.FromDateTime(p.ReversedOn ?? p.PaymentDate) : null))];

            var (labels, current, previous) = DelinquencyHistory.Trend(DateOnly.FromDateTime(DateTime.UtcNow), Days, loans, payments);

            return new DelinquencyTrendResponse(labels, current, previous);
        }

        private sealed class LoanRow
        {
            public Guid Id { get; set; }
            public DateOnly DisbursementDate { get; set; }
            public DateOnly MaturityDate { get; set; }
            public int TermMonths { get; set; }
            public decimal InstallmentAmount { get; set; }
        }

        private sealed class PaymentRow
        {
            public Guid LoanId { get; set; }
            public DateTime PaymentDate { get; set; }
            public decimal Amount { get; set; }
            public DateTime? ReversedOn { get; set; }
            public PaymentStatus Status { get; set; }
        }
    }
}
