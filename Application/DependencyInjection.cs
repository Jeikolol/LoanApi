using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(Application.Behaviors.AuditBehavior<,>));
            });

            services.AddScoped<LoanValidationService>();
            services.AddScoped<DelinquencyService>();
            services.AddScoped<CreditScoreService>();
            services.AddScoped<IFileStorageFactory, FileStorageFactory>();
            services.AddScoped<Application.Features.Delinquency.ILateFeeCharger, Application.Features.Delinquency.LateFeeCharger>();
            services.AddScoped<Application.Features.Delinquency.ILateFeeSweep, Application.Features.Delinquency.LateFeeSweep>();
            services.AddScoped<Application.Features.Delinquency.IDelinquencyRefresher, Application.Features.Delinquency.DelinquencyRefresher>();
            services.AddScoped<Application.Features.Employees.IEmployeeAccessValidator, Application.Features.Employees.EmployeeAccessValidator>();

            return services;
        }
    }
}
