using Application.Exceptions;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Features.Delinquency
{
    /// <summary>The outcome of charging every overdue loan.</summary>
    public record LateFeeSweepResponse(int LoansChecked, int LoansCharged, int DaysCharged, decimal AmountCharged, int Skipped, int Failed, bool Automatic);

    public interface ILateFeeSweep
    {
        /// <summary>
        /// Charges every overdue loan that is being collected. The automatic (scheduled) run only happens when the
        /// active delinquency policy allows it; an administrator's manual run always goes ahead.
        /// </summary>
        Task<LateFeeSweepResponse> RunAsync(bool automatic, CancellationToken cancellationToken = default);
    }

    public sealed class LateFeeSweep : ILateFeeSweep
    {
        private readonly IDbConnection _connection;
        private readonly ILateFeeCharger _charger;
        private readonly ILogger<LateFeeSweep> _logger;

        public LateFeeSweep(IDbConnection connection, ILateFeeCharger charger, ILogger<LateFeeSweep> logger)
        {
            _connection = connection;
            _charger = charger;
            _logger = logger;
        }

        public async Task<LateFeeSweepResponse> RunAsync(bool automatic, CancellationToken cancellationToken = default)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (automatic)
            {
                bool? allowed = await _connection.QueryFirstOrDefaultAsync<bool?>(
                    @"SELECT AutomaticallyChargeLateFees FROM [Loans].[DelinquencyPolicy]
                      WHERE IsActive = 1 AND IsDeleted = 0 ORDER BY CreatedOn DESC");

                if (allowed != true)
                {
                    _logger.LogInformation("Automatic late fees are switched off by the delinquency policy; nothing charged");
                    return new LateFeeSweepResponse(0, 0, 0, 0m, 0, 0, Automatic: true);
                }
            }

            int[] collectable = Enum.GetValues<LoanStatus>().Where(DelinquencyRules.IsCollectable).Select(status => (int)status).ToArray();

            // Loans already charged through today are left out, so a repeated run is cheap.
            List<Guid> candidates = (await _connection.QueryAsync<Guid>(
                @"SELECT Id FROM [Loans].[Loan]
                  WHERE IsDeleted = 0 AND LoanStatus IN @Statuses AND NextPaymentDueDate < @Today
                    AND (LateFeesChargedThrough IS NULL OR LateFeesChargedThrough < @Today)
                  ORDER BY NextPaymentDueDate",
                new { Statuses = collectable, Today = today })).ToList();

            int charged = 0, days = 0, skipped = 0, failed = 0;
            decimal amount = 0m;

            foreach (Guid loanId in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    LateFeeChargeResponse result = await _charger.ChargeAsync(loanId, today, cancellationToken);
                    if (result.DaysCharged > 0)
                    {
                        charged++;
                        days += result.DaysCharged;
                        amount += result.AmountCharged;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch (CustomException ex)
                {
                    // Not chargeable any more (paid in the meantime) or charged by a concurrent run: not a failure.
                    skipped++;
                    _logger.LogInformation("Loan {LoanId} skipped: {Reason}", loanId, ex.Message);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One bad loan must not stop the others.
                    failed++;
                    _logger.LogError(ex, "Late fees could not be charged to loan {LoanId}", loanId);
                }
            }

            _logger.LogInformation(
                "Late fee sweep ({Mode}): {Checked} loan(s) checked, {Charged} charged ({Days} day(s), {Amount}), {Skipped} skipped, {Failed} failed",
                automatic ? "automatic" : "manual", candidates.Count, charged, days, amount, skipped, failed);

            return new LateFeeSweepResponse(candidates.Count, charged, days, amount, skipped, failed, automatic);
        }
    }

    /// <summary>The "charge all" action an administrator can trigger by hand.</summary>
    public record ChargeAllLateFeesCommand : IRequest<LateFeeSweepResponse>;

    public class ChargeAllLateFeesCommandHandler : IRequestHandler<ChargeAllLateFeesCommand, LateFeeSweepResponse>
    {
        private readonly ILateFeeSweep _sweep;

        public ChargeAllLateFeesCommandHandler(ILateFeeSweep sweep) => _sweep = sweep;

        public Task<LateFeeSweepResponse> Handle(ChargeAllLateFeesCommand request, CancellationToken cancellationToken) =>
            _sweep.RunAsync(automatic: false, cancellationToken);
    }

    /// <summary>When a daily job runs next.</summary>
    public static class DailySchedule
    {
        public static readonly TimeSpan DefaultRunAtUtc = new(6, 0, 0);

        /// <summary>"HH:mm" as a time of day, or false when it is not one.</summary>
        public static bool TryParseTime(string? text, out TimeSpan time)
        {
            time = default;
            return TimeSpan.TryParseExact(text, @"h\:mm", System.Globalization.CultureInfo.InvariantCulture, out time)
                && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
        }

        /// <summary>The next moment (UTC) at that time of day, strictly after the given instant.</summary>
        public static DateTime NextRunUtc(DateTime nowUtc, TimeSpan runAtUtc)
        {
            DateTime today = DateTime.SpecifyKind(nowUtc.Date + runAtUtc, DateTimeKind.Utc);
            return today > nowUtc ? today : today.AddDays(1);
        }
    }
}
