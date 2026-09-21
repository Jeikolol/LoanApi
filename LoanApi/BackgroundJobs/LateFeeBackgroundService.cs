using Application.Features.Delinquency;

namespace LoanApi.BackgroundJobs
{
    /// <summary>
    /// The daily job, at <c>LateFees:AutoCharge:RunAtUtc</c>. First it refreshes the days overdue and status of every loan
    /// being repaid (on unless <c>Delinquency:DailyRefresh:Enabled</c> is false; it moves no money). Then it charges the
    /// late fees of every overdue loan, only when <c>LateFees:AutoCharge:Enabled</c> is true and the active delinquency
    /// policy allows it. Running twice (a restart, or two instances of the API) is harmless.
    /// </summary>
    public sealed class LateFeeBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LateFeeBackgroundService> _logger;

        public LateFeeBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<LateFeeBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>Runs one step; false when the application is stopping. Any other failure is logged and the next day tries again.</summary>
        private async Task<bool> RunAsync<TService>(Func<TService, CancellationToken, Task> step, string name, CancellationToken stoppingToken)
            where TService : notnull
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                await step(scope.ServiceProvider.GetRequiredService<TService>(), stoppingToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The automatic {Step} failed", name);
                return true;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool refresh = _configuration.GetValue("Delinquency:DailyRefresh:Enabled", true);
            bool charge = _configuration.GetValue<bool>("LateFees:AutoCharge:Enabled");
            if (!refresh && !charge)
            {
                _logger.LogInformation("The daily delinquency job is disabled (Delinquency:DailyRefresh:Enabled and LateFees:AutoCharge:Enabled are both off)");
                return;
            }

            _logger.LogInformation("Daily delinquency job: refresh {Refresh}, late fee charging {Charge}", refresh, charge);

            string? configured = _configuration["LateFees:AutoCharge:RunAtUtc"];
            TimeSpan runAt = DailySchedule.DefaultRunAtUtc;
            if (configured is not null && !DailySchedule.TryParseTime(configured, out runAt))
            {
                _logger.LogWarning("LateFees:AutoCharge:RunAtUtc '{Value}' is not a time like 06:00; using {Default}", configured, DailySchedule.DefaultRunAtUtc);
                runAt = DailySchedule.DefaultRunAtUtc;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime next = DailySchedule.NextRunUtc(DateTime.UtcNow, runAt);
                _logger.LogInformation("Next daily delinquency job at {Next:u}", next);

                try
                {
                    await Task.Delay(next - DateTime.UtcNow, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Each step has its own scope and its own failure handling: a failed refresh must not skip the fees.
                if (refresh && !await RunAsync<IDelinquencyRefresher>(
                        (service, token) => service.RefreshAsync(DateOnly.FromDateTime(DateTime.UtcNow), token), "delinquency refresh", stoppingToken))
                {
                    break;
                }

                if (charge && !await RunAsync<ILateFeeSweep>((service, token) => service.RunAsync(automatic: true, token), "late fee charge", stoppingToken))
                {
                    break;
                }
            }
        }
    }
}
