using Application.Features.Payments;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency
{
    /// <summary>What refreshing the delinquency of every loan being repaid changed.</summary>
    public record DelinquencyRefreshResponse(int LoansChecked, int Updated, int NowDelinquent, int BackToActive);

    public interface IDelinquencyRefresher
    {
        /// <summary>
        /// Brings the days overdue, the delinquency percentage and the status of every loan being repaid up to date for
        /// <paramref name="today"/>. It moves no money and never writes a loan off. Safe to repeat.
        /// </summary>
        Task<DelinquencyRefreshResponse> RefreshAsync(DateOnly today, CancellationToken cancellationToken = default);
    }

    public sealed class DelinquencyRefresher : IDelinquencyRefresher
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<DelinquencyRefresher> _logger;

        public DelinquencyRefresher(IDbConnection connection, ILogger<DelinquencyRefresher> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<DelinquencyRefreshResponse> RefreshAsync(DateOnly today, CancellationToken cancellationToken = default)
        {
            decimal dailyRate = await _connection.QueryFirstOrDefaultAsync<decimal?>(
                @"SELECT DailyPenaltyRate FROM [Loans].[DelinquencyPolicy]
                  WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CreatedOn DESC") ?? LateFeeCharger.DefaultDailyRate;

            int[] collectable = Enum.GetValues<LoanStatus>().Where(DelinquencyRules.IsCollectable).Select(status => (int)status).ToArray();

            List<LoanRow> loans = (await _connection.QueryAsync<LoanRow>(
                @"SELECT Id, LoanStatus, NextPaymentDueDate, DaysOverdue, DelinquencyPercentage
                  FROM [Loans].[Loan] WHERE IsDeleted = 0 AND LoanStatus IN @Statuses",
                new { Statuses = collectable })).ToList();

            int updated = 0, nowDelinquent = 0, backToActive = 0;

            foreach (LoanRow loan in loans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int days = PaymentRules.DaysOverdue(loan.NextPaymentDueDate, today);
                decimal percentage = LateFeeRules.DelinquencyPercentage(days, dailyRate);
                LoanStatus status = days > 0
                    ? DelinquencyRules.NextStatus(loan.LoanStatus, days)
                    : loan.LoanStatus == LoanStatus.Defaulted ? LoanStatus.Active : loan.LoanStatus;

                if (days == loan.DaysOverdue && percentage == loan.DelinquencyPercentage && status == loan.LoanStatus) continue;

                // The status guard leaves a loan alone when a payment changed it while we were reading.
                int rows = await _connection.ExecuteAsync(
                    @"UPDATE [Loans].[Loan]
                      SET DaysOverdue = @Days, DelinquencyPercentage = @Percentage, LoanStatus = @Status, UpdatedOn = @Now
                      WHERE Id = @Id AND IsDeleted = 0 AND LoanStatus = @Current",
                    new { loan.Id, Days = days, Percentage = percentage, Status = (int)status, Current = (int)loan.LoanStatus, Now = DateTime.UtcNow });

                if (rows != 1) continue;

                updated++;
                if (status == LoanStatus.Defaulted && loan.LoanStatus != LoanStatus.Defaulted) nowDelinquent++;
                if (status == LoanStatus.Active && loan.LoanStatus == LoanStatus.Defaulted) backToActive++;
            }

            _logger.LogInformation(
                "Delinquency refresh: {Checked} loan(s) checked, {Updated} updated, {Delinquent} now delinquent, {Active} back to active",
                loans.Count, updated, nowDelinquent, backToActive);

            return new DelinquencyRefreshResponse(loans.Count, updated, nowDelinquent, backToActive);
        }

        private sealed class LoanRow
        {
            public Guid Id { get; set; }
            public LoanStatus LoanStatus { get; set; }
            public DateOnly NextPaymentDueDate { get; set; }
            public int DaysOverdue { get; set; }
            public decimal DelinquencyPercentage { get; set; }
        }
    }

    /// <summary>Refreshes the delinquency of every loan by hand (administrators and managers).</summary>
    public record RefreshDelinquencyCommand : IRequest<DelinquencyRefreshResponse>;

    public class RefreshDelinquencyCommandHandler : IRequestHandler<RefreshDelinquencyCommand, DelinquencyRefreshResponse>
    {
        private readonly IDelinquencyRefresher _refresher;

        public RefreshDelinquencyCommandHandler(IDelinquencyRefresher refresher) => _refresher = refresher;

        public Task<DelinquencyRefreshResponse> Handle(RefreshDelinquencyCommand request, CancellationToken cancellationToken) =>
            _refresher.RefreshAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
    }
}
