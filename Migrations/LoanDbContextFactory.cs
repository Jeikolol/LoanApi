using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Persistence;


namespace Migrations
{
    public class LoanDbContextFactory : IDesignTimeDbContextFactory<LoanDbContext>
    {
        public LoanDbContext CreateDbContext(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("database.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("LoanDatabase");

            var opts = new DbContextOptionsBuilder<LoanDbContext>()
                .UseSqlServer(connectionString, b =>
                {
                    b.MigrationsAssembly("Migrations");
                    b.MigrationsHistoryTable("__EFMigrationsHistory");
                })
                .Options;

            return new LoanDbContext(opts);
        }
    }

}
