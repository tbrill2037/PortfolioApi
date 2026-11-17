using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Portfolio.Infra.AWS.Configuration;

namespace Portfolio.Infra.AWS.Messaging;

/// <summary>
/// Provides SQS-based email queue functionality for persisting and dispatching email messages.
/// </summary>
public sealed class EmailQueue : IEmailQueue
{
    private readonly ILogger<EmailQueue> _logger;
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailOutboxRepository _outboxRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsOptions _sqsOptions;

    public EmailQueue(
        ILogger<EmailQueue> logger,
        IEmailRepository emailRepository,
        IEmailOutboxRepository outboxRepository,
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IAmazonSQS sqsClient,
        IOptions<SqsOptions> sqsOptions)
    {
        _logger = logger;
        _emailRepository = emailRepository;
        _outboxRepository = outboxRepository;
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _sqsClient = sqsClient;
        _sqsOptions = sqsOptions.Value;
    }

    public async Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Operation] = LoggingOperations.QueueEmail,
            [LoggingProperties.EmailId] = message.Id,
            [LoggingProperties.CorrelationId] = message.CorrelationId,
            [LoggingProperties.CampaignId] = message.CampaignId,
            [LoggingProperties.EmailTemplateType] = message.TemplateType,
            [LoggingProperties.RequestId] = _httpContextAccessor.HttpContext?.TraceIdentifier,
        });

        _logger.LogInformation("{EventName} | Queue request received.", EmailLogEvents.Queue.QueueRequested);
        _logger.LogInformation("{EventName} | Persisting email.", EmailLogEvents.Queue.PersistingEmail);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var emailEntity = new EmailMessageEntity
                {
                    Id = message.Id,
                    CorrelationId = message.CorrelationId,
                    CampaignId = message.CampaignId,
                    Recipient = message.RecipientEmail.Address,
                    RecipientName = message.RecipientEmail.DisplayName,
                    Sender = message.SenderEmail.Address,
                    SenderName = message.SenderEmail.DisplayName,
                    Subject = message.Subject,
                    TemplateType = message.TemplateType,
                    TemplatePath = message.TemplatePath,
                    Status = EmailStatus.Pending,
                    RetryCount = 0,
                    CreatedUtc = DateTime.UtcNow,
                };

                await _emailRepository.AddAsync(emailEntity, cancellationToken);

                var outbox = new EmailOutboxEntity
                {
                    Id = Guid.NewGuid(),
                    EmailMessageId = message.Id,
                    PayloadJson = JsonSerializer.Serialize(message),
                    CreatedUtc = DateTime.UtcNow,
                };

                await _outboxRepository.AddAsync(outbox, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "{EventName} | Email persisted in outbox.",
                    EmailLogEvents.Queue.EmailPersisted);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{EventName} | Failed to persist email in outbox.",
                    EmailLogEvents.Queue.PersistenceFailed);

                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        });

        try
        {
            await NotifyOutboxWorkerAsync(message.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to notify outbox worker for email {EmailId}. The email remains persisted and will be picked up by scheduled reconciliation.",
                message.Id);
        }
    }

    private async Task NotifyOutboxWorkerAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _sqsOptions.OutboxQueueUrl,
            MessageBody = JsonSerializer.Serialize(new
            {
                EmailId = emailId,
                EventType = "ProcessOutbox"
            })
        };

        await _sqsClient.SendMessageAsync(request, cancellationToken);

        _logger.LogInformation("Outbox worker notified for email {EmailId}.", emailId);
    }
}