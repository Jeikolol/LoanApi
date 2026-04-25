using Application;
using Asp.Versioning.Routing;
using Infrastructure;
using LoanApi.Configurations;
using Persistence;
using System.Reflection;
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

// Add Swagger/OpenAPI
services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitSeeder();
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

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
