using Application.Interfaces;
using Dapper;
using Infrastructure.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using System.Data;
using System.Reflection;
namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services
                .AddHttpContextAccessor()
                .AddDatabase(configuration)
                .AddPersistence(configuration)
                .AddServices();

            return services;
        }

        public static async Task InitSeeder(this IServiceProvider service)
        {
            var init = service.GetRequiredService<ApplicationDbInitializer>();
            await init.InitializeAsync(CancellationToken.None);
        }

        /// <summary>
        /// Configures database access (EF Core and Dapper)
        /// </summary>
        private static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("LoanDatabase");

            services.AddDbContext<LoanDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly("Migrations");
                    sql.MigrationsHistoryTable("__EFMigrationsHistory");
                });
            })
            .AddScoped<ApplicationDbInitializer>()
            .AddScoped<ApplicationDbSeeder>();

            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            });

            SqlMapper.AddTypeHandler(new DateOnlyHandler());
            SqlMapper.AddTypeHandler(new NullableDateOnlyHandler());
            SqlMapper.AddTypeHandler(new TimeOnlyHandler());
            SqlMapper.AddTypeHandler(new NullableTimeOnlyHandler());

            return services;
        }

        internal static IServiceCollection AddServices(this IServiceCollection services) =>
            services
                .AddServices(typeof(ITransientService), ServiceLifetime.Transient)
                .AddServices(typeof(IScopedService), ServiceLifetime.Scoped)
                .AddServices(typeof(ISingletonService), ServiceLifetime.Singleton);
        internal static IServiceCollection AddServices(
            this IServiceCollection services,
            Type interfaceType,
            ServiceLifetime lifetime)
        {
            var assembliesToScan = new[]
            {
                typeof(Infrastructure.DependencyInjection).Assembly,
                typeof(Application.Interfaces.ITransientService).Assembly,
                typeof(Persistence.DependencyInjection).Assembly
            };

            var types = assembliesToScan
                .SelectMany(a => a.DefinedTypes)
                .Where(t =>
                    interfaceType.IsAssignableFrom(t) &&
                    t.IsClass &&
                    !t.IsAbstract)
                .Select(t => new
                {
                    Service = t.ImplementedInterfaces
                        .FirstOrDefault(i => interfaceType.IsAssignableFrom(i)),
                    Implementation = t.AsType()
                })
                .Where(t => t.Service != null);

            foreach (var type in types)
            {
                services.AddService(type.Service!, type.Implementation, lifetime);
            }

            return services;
        }


        internal static IServiceCollection AddService(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime) =>
            lifetime switch
            {
                ServiceLifetime.Transient => services.AddTransient(serviceType, implementationType),
                ServiceLifetime.Scoped => services.AddScoped(serviceType, implementationType),
                ServiceLifetime.Singleton => services.AddSingleton(serviceType, implementationType),
                _ => throw new ArgumentException("Invalid lifeTime", nameof(lifetime))
            };

        private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
        {
            public override void SetValue(IDbDataParameter p, DateOnly value)
            {
                p.Value = value.ToDateTime(TimeOnly.MinValue);
                p.DbType = DbType.Date;
            }
            public override DateOnly Parse(object value) =>
                value is DateTime dt ? DateOnly.FromDateTime(dt)
                                     : DateOnly.Parse((string)value);
        }

        private sealed class NullableDateOnlyHandler : SqlMapper.TypeHandler<DateOnly?>
        {
            public override void SetValue(IDbDataParameter p, DateOnly? value)
            {
                p.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
                p.DbType = DbType.Date;
            }
            public override DateOnly? Parse(object value) =>
                value is null or DBNull ? null : DateOnly.FromDateTime((DateTime)value);
        }

        private sealed class TimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly>
        {
            public override void SetValue(IDbDataParameter p, TimeOnly value)
            {
                p.Value = value.ToTimeSpan();
                p.DbType = DbType.Time;
            }
            public override TimeOnly Parse(object value) =>
                value is TimeSpan ts ? TimeOnly.FromTimeSpan(ts)
                                     : TimeOnly.Parse((string)value);
        }

        private sealed class NullableTimeOnlyHandler : SqlMapper.TypeHandler<TimeOnly?>
        {
            public override void SetValue(IDbDataParameter p, TimeOnly? value)
            {
                p.Value = value.HasValue ? value.Value.ToTimeSpan() : DBNull.Value;
                p.DbType = DbType.Time;
            }
            public override TimeOnly? Parse(object value) =>
                value is null or DBNull ? null : TimeOnly.FromTimeSpan((TimeSpan)value);
        }
    }
}
