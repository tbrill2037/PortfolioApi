using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application;
using Portfolio.Application.Common.Logging;
using Portfolio.Infra;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.Extensions;

namespace Portfolio.EmailEventWorker;

/// <summary>
/// Provides dependency injection and application configuration setup for the Lambda worker.
/// </summary>
public static class Startup
{
    public static async Task<IServiceProvider> ConfigureAsync()
    {
        var configuration = new ConfigurationManager();

        configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        // Load secrets from AWS Secrets Manager into the configuration.
        await SecretInitializer.LoadAsync(configuration, configuration["AWS:Secrets:SecretName"]);

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddStructuredLogging(configuration, WorkerNames.EmailEventWorker);
        services.AddApplicationServices(configuration);
        services.AddEmailWorkerInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}