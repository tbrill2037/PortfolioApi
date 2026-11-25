using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Messaging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Email.Configuration;
using Portfolio.Infra.Email.Models;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Creates and queues outbound email messages.
/// </summary>
public sealed class EmailDispatcher : IEmailDispatcher
{
    private readonly IEmailQueue _queue;
    private readonly EmailOptions _options;
    private readonly ICorrelationIdGenerator _idGenerator;
    private readonly IEmailCooldownService _cooldownService;
    private readonly IClientIpService _clientIpService;

    public EmailDispatcher(
        IEmailQueue queue,
        IOptions<EmailOptions> options,
        ICorrelationIdGenerator idGenerator,
        IEmailCooldownService cooldownService,
        IClientIpService clientIpService)
    {
        _queue = queue;
        _options = options.Value;
        _idGenerator = idGenerator;
        _cooldownService = cooldownService;
        _clientIpService = clientIpService;
    }

    /// <summary>
    /// Creates and queues a contact request email.
    /// </summary>
    public async Task QueueContactEmailAsync(
        ContactEmailRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var clientIp = _clientIpService.GetClientIp();

        await _cooldownService.CheckAsync(
            new EmailCooldownContext
            {
                EmailAddress = request.Email,
                IpAddress = clientIp,
                EmailType = EmailType.Contact,
            },
            cancellationToken);

        var email = new EmailMessage
        {
            CorrelationId = _idGenerator.CreateCorrelationId(),
            
            RecipientEmail = new EmailAddress
            {
                Address = _options.FromEmail,
                DisplayName = _options.FromName
            },

            SenderEmail = new EmailAddress
            {
                Address = request.Email,
                DisplayName = request.Name
            },

            Subject = EmailTemplateSubjects.Contact,
            TemplateType = EmailType.Contact,
            TemplatePath = EmailTemplatePaths.Contact,

            TemplateModel = new ContactEmailModel
            {
                Name = request.Name,
                Email = request.Email,
                Message = request.Message
            },

            Resources =
            [
                EmailResourceFactory.CreateEmbedded("logo.png", "logo"),
                EmailResourceFactory.CreateEmbedded("logo.png", "portfolio-icon"),
                EmailResourceFactory.CreateEmbedded("github.png", "github-icon"),
                EmailResourceFactory.CreateEmbedded("linkedin.png", "linkedin-icon"),
                EmailResourceFactory.CreateEmbedded("stackoverflow.png", "stackoverflow-icon")
            ]
        };

        await _queue.EnqueueAsync(
            email,
            cancellationToken);
    }
}