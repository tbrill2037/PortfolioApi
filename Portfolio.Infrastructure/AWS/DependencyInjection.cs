using Amazon.SecretsManager;
using Amazon.SimpleEmailV2;
using Amazon.SQS;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Infra.AWS.Configuration;

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

        services.Configure<SqsOptions>(configuration.GetSection(SqsOptions.SectionName));
        services.Configure<SesOptions>(configuration.GetSection(SesOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        return services;
    }
}