using Application;
using Asp.Versioning.Routing;
using Infrastructure;
using LoanApi.Configurations;
using Persistence;
using System.Reflection;

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
// Add services to the container.

services.AddControllers();

builder.AddConfigurations();

services.Configure<DatabaseSettings>(config);
services.Configure<SecuritySettings>(config);
services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap["apiVersion"] = typeof(ApiVersionRouteConstraint);
});

services.AddInfrastructure(config);
services.AddApplication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitSeeder();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//}

app.UsePersistence();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
