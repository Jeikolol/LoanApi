using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using System.Data;

namespace Infrastructure.Extensions
{
    public static class SeedExtensions
    {
        public static async Task SeedDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
            var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            // Create database if it doesn't exist
            await context.Database.MigrateAsync();

            // Use ApplicationDbSeederSecure for high-performance bulk insert with actual FK references
            var seeder = new ApplicationDbSeeder(connection);
            await seeder.SeedDatabaseAsync(context, CancellationToken.None);
        }
    }
}
