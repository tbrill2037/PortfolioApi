using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Lambda;
using Portfolio.Application.Common.Parsing.Email;
using Portfolio.Infra.AWS.Messaging;
using Portfolio.Infra.Email.Configuration;
using Portfolio.Infra.Email.Interfaces;
using Portfolio.Infra.Services.Email;
using Portfolio.Infra.Services.Email.Resources;
using Portfolio.Infra.Services.Lambda;

namespace Portfolio.Infra.Email;

/// <summary>
/// Provides dependency injection registrations for email processing services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers email processing services and related configuration.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEmailViewRenderer, ScribanViewRenderer>();
        services.AddScoped<IEmailTemplateLoader, EmbeddedEmailTemplateLoader>();
        services.AddScoped<IEmailTemplateModelResolver, EmailTemplateModelResolver>();
        services.AddScoped<IEmailComposer, EmailComposer>();
        services.AddScoped<IEmailSender, SesEmailSender>();
        services.AddScoped<IEmailQueue, EmailQueue>();

        services.AddSingleton<ISesRateLimiter, SesRateLimiter>();
        services.AddSingleton<IEmailRateLimiter, EmailRateLimiter>();

        services.AddScoped<IEmailQuotaService, EmailQuotaService>();
        services.AddScoped<IEmailCooldownService, EmailCooldownService>();
        services.AddScoped<IEmailDispatcher, EmailDispatcher>();
        services.AddScoped<IEmailProcessor, EmailProcessor>();
        services.AddScoped<IEmailEventProcessor, EmailEventProcessor>();
        services.AddScoped<IEmailDeadLetterArchiver, EmailDeadLetterArchiver>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();

        services.AddSingleton<ISesEventParser, SesEventParser>();

        services.AddScoped<IEmailResourceProvider, EmbeddedEmailResourceProvider>();
        services.AddScoped<IEmailResourceProvider, S3EmailResourceProvider>();
        services.AddScoped<IEmailResourceResolver, EmailResourceResolver>();

        services.AddSingleton<ILambdaExecutionTimeoutProvider, LambdaExecutionTimeoutProvider>();

        services.Configure<EmailBrandingOptions>(configuration.GetSection(EmailBrandingOptions.SectionName));

        return services;
    }
}