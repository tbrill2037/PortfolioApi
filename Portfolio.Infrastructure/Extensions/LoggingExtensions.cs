using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace Portfolio.Infra.Extensions;

/// <summary>
/// Provides extension methods for configuring application logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Registers structured logging using Serilog.
    /// </summary>
    public static IServiceCollection AddStructuredLogging(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        return services;
    }
}