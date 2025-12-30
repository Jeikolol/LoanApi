using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrchardCore.Localization;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Persistence
{
    public static class DependencyInjection
    {
        private const string CorsPolicy = nameof(CorsPolicy);
        public static IServiceCollection AddPersistence(this IServiceCollection service, IConfiguration config)
        {
            var jwt = config.GetSection("JwtSettings").Get<JwtSettings>()!;

            service
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.Key)
                        ),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            service.AddCorsPolicy(config);
            service.AddAuthorization();
            service.AddVersioning();
            service.AddSwagger();
            service.AddMemoryCache();
            service.AddPOLocalization(config);

            return service;
        }

        public static IApplicationBuilder UsePersistence(this IApplicationBuilder app)
        {
            app.UseCorsPolicy();
            app.UseSwaggerVersion();

            return app;
        }

        public static IServiceCollection AddVersioning(this IServiceCollection service)
        {
            service.AddApiVersioning(opt =>
            {
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            return service;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection service)
        {
            service.AddSwaggerGen();

            return service;
        }

        public static IApplicationBuilder UseSwaggerVersion(this IApplicationBuilder app)
        {
            var provider = app.ApplicationServices
                .GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.DocumentTitle = "Loan System API";
                options.RoutePrefix = "swagger";

                foreach (var description in provider.ApiVersionDescriptions.Select(x => x.GroupName))
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description}/swagger.json",
                        $"Loan System API {description.ToUpperInvariant()}"
                    );
                }

                options.DefaultModelsExpandDepth(-1);
                options.DisplayRequestDuration();
                options.EnablePersistAuthorization();
            });

            return app;
        }

        internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
        {
            var corsSettings = config.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
            var origins = new List<string>();

            if (corsSettings.React is not null)
                origins.AddRange(corsSettings.React.Split(';', StringSplitOptions.RemoveEmptyEntries));

            return services.AddCors(opt =>
                opt.AddPolicy(CorsPolicy, policy =>
                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins(origins.ToArray())));
        }

        internal static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) =>
            app.UseCors(CorsPolicy);

        internal static IServiceCollection AddPOLocalization(this IServiceCollection services, IConfiguration config)
        {
            var localizationSettings = config.GetSection(nameof(LocalizationSettings)).Get<LocalizationSettings>();

            if (localizationSettings?.EnableLocalization is true
                && localizationSettings.ResourcesPath is not null)
            {
                services.AddPortableObjectLocalization(options => options.ResourcesPath = localizationSettings.ResourcesPath);

                services.Configure<RequestLocalizationOptions>(options =>
                {
                    if (localizationSettings.SupportedCultures != null)
                    {
                        var supportedCultures = localizationSettings.SupportedCultures.Select(x => new CultureInfo(x)).ToList();

                        options.SupportedCultures = supportedCultures;
                        options.SupportedUICultures = supportedCultures;
                    }

                    options.DefaultRequestCulture = new RequestCulture(localizationSettings.DefaultRequestCulture ?? "en-US");
                    options.FallBackToParentCultures = localizationSettings.FallbackToParent ?? true;
                    options.FallBackToParentUICultures = localizationSettings.FallbackToParent ?? true;
                });

                services.AddSingleton<ILocalizationFileLocationProvider, PoFileLocationProvider>();
            }

            return services;
        }
    }
}
