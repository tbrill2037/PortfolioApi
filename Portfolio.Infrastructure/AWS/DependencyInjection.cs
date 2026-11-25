using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleEmailV2;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Execution;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Messaging;
using Portfolio.Infra.AWS.Services.Email;
using Portfolio.Infra.AWS.Services.Lambda;

namespace Portfolio.Infra.AWS;

/// <summary>
/// Provides AWS service registrations for dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers AWS SDK services and related configuration options.
    /// </summary>
    public static IServiceCollection AddAws(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAWSService<IAmazonSimpleEmailServiceV2>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonSecretsManager>();

        services.AddScoped<IEmailSender, SesEmailSender>();
        services.AddScoped<IEmailOutboxNotifier, SqsOutboxNotifier>();
        services.AddScoped<IEmailQueuePublisher, SqsEmailQueuePublisher>();
        services.AddScoped<IEmailQuotaService, SesQuotaService>();
        services.AddScoped<IEmailResourceProvider, S3EmailResourceProvider>();
        services.AddSingleton<IEmailProviderRateLimiter, SesRateLimiter>();
        services.AddSingleton<IEmailEventParser, SesEventParser>();
        services.AddSingleton<IExecutionTimeoutProvider, LambdaExecutionTimeoutProvider>();

        services.Configure<SqsOptions>(configuration.GetSection(SqsOptions.SectionName));
        services.Configure<SesOptions>(configuration.GetSection(SesOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        return services;
    }
}