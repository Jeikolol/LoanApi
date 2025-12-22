using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;

namespace Infrastructure.DataAccess
{
    public class ApplicationDbInitializer
    {
        private readonly ApplicationDbSeeder _dbSeeder;
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<ApplicationDbInitializer> _logger;

        public ApplicationDbInitializer(ApplicationDbSeeder dbSeeder, LoanDbContext dbContext, ILogger<ApplicationDbInitializer> logger)
        {
            _dbSeeder = dbSeeder;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("=== Database Initialization Started ===");

                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    _logger.LogError("❌ Cannot connect to database");
                    return;
                }

                _logger.LogInformation("Database connection successful");

                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                var appliedMigrations = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);

                _logger.LogInformation("Migration Status:");
                _logger.LogInformation("   - Applied migrations: {Count}", appliedMigrations.Count());
                _logger.LogInformation("   - Pending migrations: {Count}", pendingMigrations.Count());

                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    foreach (var migration in pendingMigrations)
                    {
                        _logger.LogInformation("   - {Migration}", migration);
                    }

                    await _dbContext.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation(" All migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("Database schema is up to date");
                }

                try
                {
                    _logger.LogInformation(" Starting database seeding...");
                    await _dbSeeder.SeedDatabaseAsync(_dbContext, cancellationToken);
                    _logger.LogInformation("Database seeding completed");
                }
                catch (Exception seedEx)
                {
                    _logger.LogWarning(seedEx, "Database seeding failed, but application will continue");
                }

                _logger.LogInformation("=== Database Initialization Completed Successfully ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during database initialization");

                if (ex.Message.Contains("already an object named"))
                {
                    _logger.LogError("DIAGNOSIS: Database schema is out of sync with migrations.");
                    _logger.LogError("SOLUTION OPTIONS:");
                    _logger.LogError("   1. Delete the problematic tables manually and restart");
                    _logger.LogError("   3. Reset migrations: dotnet ef database update 0 && dotnet ef database update");
                    _logger.LogError("   4. Check if __EFMigrationsHistory table is consistent");
                }
                else if (ex.Message.Contains("connection"))
                {
                    _logger.LogError("DIAGNOSIS: Database connection issue");
                    _logger.LogError("CHECK: Connection string, SQL Server running, network access");
                }

                throw;
            }
        }

    }
}
