namespace LoanApi.Configurations
{
    internal static class DependencyInjection
    {
        internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
        {
            const string configurationsDirectory = "Configurations";
            var env = builder.Environment;

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/database.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/database.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/security.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/security.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/cors.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/cors.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/localization.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"{configurationsDirectory}/localization.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            return builder;
        }
    }
}
