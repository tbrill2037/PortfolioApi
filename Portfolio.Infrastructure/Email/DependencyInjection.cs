using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Infra.Email.Configuration;
using Portfolio.Infra.Email.Interfaces;
using Portfolio.Infra.Services.Email;

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
        services.AddScoped<IEmailQueue, EmailQueue>();
        services.AddScoped<IEmailComposer, EmailComposer>();
        services.AddScoped<IEmailCooldownService, EmailCooldownService>();
        services.AddScoped<IEmailDispatcher, EmailDispatcher>();
        services.AddScoped<IEmailProcessor, EmailProcessor>();
        services.AddScoped<IEmailEventProcessor, EmailEventProcessor>();
        services.AddScoped<IEmailDeadLetterArchiver, EmailDeadLetterArchiver>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
        services.AddScoped<IEmailResourceProvider, EmbeddedEmailResourceProvider>();
        services.AddScoped<IEmailResourceResolver, EmailResourceResolver>();
        services.AddSingleton<IEmailRateLimiter, EmailRateLimiter>();

        services.Configure<EmailOptions>(
            configuration.GetSection(EmailOptions.SectionName));
        services.Configure<EmailProcessingOptions>(
            configuration.GetSection(EmailProcessingOptions.SectionName));
        services.Configure<EmailBrandingOptions>(
            configuration.GetSection(EmailBrandingOptions.SectionName));

        return services;
    }
}