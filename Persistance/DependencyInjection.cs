using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection service, IConfiguration config)
        {
            var jwt = config.GetSection("JwtSettings").Get<SecuritySettings>()!;

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

            service.AddAuthorization();
            service.AddVersioning();
            service.AddSwagger();
         
            return service;
        }

        public static IApplicationBuilder UsePersistence(this IApplicationBuilder app)
        {
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
    }
}
