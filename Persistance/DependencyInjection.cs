using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection service, IConfiguration config)
        {
            service.AddDbContext<LoanDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("LoanDatabase"),
                     b => b.MigrationsAssembly("Migrations")
                )
            );

            return service;
        }
    }
}
