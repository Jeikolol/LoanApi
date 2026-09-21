using Application;
using Asp.Versioning.Routing;
using Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using LoanApi.Configurations;
using Persistence;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;

AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
{
    if (eventArgs.Exception is ReflectionTypeLoadException ex)
    {
        Console.WriteLine("=== ReflectionTypeLoadException ===");

        foreach (var loaderException in ex.LoaderExceptions)
        {
            Console.WriteLine(loaderException?.Message);
        }
    }
};
var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var config = builder.Configuration;

services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.AddConfigurations();

services.Configure<DatabaseSettings>(config);
services.Configure<JwtSettings>(
    config.GetSection("JwtSettings")
);
services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap["apiVersion"] = typeof(ApiVersionRouteConstraint);
});

services.AddInfrastructure(config);
services.AddApplication();

// Charges late fees once a day when LateFees:AutoCharge:Enabled is true (off by default).
services.AddHostedService<LoanApi.BackgroundJobs.LateFeeBackgroundService>();

// A signed token is only accepted while the employee behind it is still active and the token is not older than the
// last deactivation, role change or password reset.
services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            string? subject = context.Principal?.FindFirst("sub")?.Value;
            long? issuedAt = long.TryParse(context.Principal?.FindFirst("iat")?.Value, out long iat) ? iat : null;

            var validator = context.HttpContext.RequestServices
                .GetRequiredService<Application.Features.Employees.IEmployeeAccessValidator>();

            if (!Guid.TryParse(subject, out Guid employeeId)
                || !await validator.IsTokenStillValidAsync(employeeId, issuedAt, context.HttpContext.RequestAborted))
            {
                context.Fail("The session is no longer valid.");
            }
        },
    };
});

// Where files are kept. Server-side only: it can hold an Azure connection string, so it never goes in the database.
services.AddSingleton(config.GetSection("Storage").Get<Application.Services.StorageConfiguration>() ?? new Application.Services.StorageConfiguration());

// Add Swagger/OpenAPI
services.AddSwaggerGen();

bool useForwardedHeaders = config.GetValue<bool>("ForwardedHeaders:Enabled");
if (useForwardedHeaders)
{
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (string proxy in config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
        }
    });
}

services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitSeeder();
}

if (useForwardedHeaders)
{
    app.UseForwardedHeaders();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LoanWeb API v1");
        options.RoutePrefix = string.Empty; // Makes Swagger UI the default page
    });
}

app.UseInfrastructure(config);

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    bool mustChangePassword = context.User.HasClaim("must_change_password", "true");
    if (mustChangePassword && !context.Request.Path.Value!.EndsWith("/change-password", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { messages = new[] { "You must change your password before continuing." } });
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
