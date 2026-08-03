using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Application.Common.Serialization;
using Portfolio.Domain.Entities;
using Portfolio.Infra.AWS.Configuration;
using System.Text.Json;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Processes pending email outbox records and publishes them to SQS.
/// </summary>
public sealed class EmailOutboxProcessor : IEmailOutboxProcessor
{
    private const int BatchSize = 20;
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly IAmazonSQS _sqs;
    private readonly SqsOptions _sqsOptions;
    private readonly ILogger<EmailOutboxProcessor> _logger;

    public EmailOutboxProcessor(
        IEmailOutboxRepository emailOutboxRepository,
        IAmazonSQS sqs,
        IOptions<SqsOptions> sqsOptions,
        ILogger<EmailOutboxProcessor> logger)
    {
        _emailOutboxRepository = emailOutboxRepository;
        _sqs = sqs;
        _sqsOptions = sqsOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Processes pending outbox messages and publishes them to the email queue.
    /// </summary>
    public async Task<EmailOutboxProcessResultModel> ProcessAsync(
        CancellationToken cancellationToken)
    {
        var messages = await _emailOutboxRepository.ClaimPendingAsync(
            BatchSize,
            DateTime.UtcNow.Add(LockDuration),
            cancellationToken);

        _logger.LogInformation(
            "{EventName} | Email outbox messages claimed. Claimed={Claimed}, LockDurationSeconds={LockDurationSeconds}",
            EmailLogEvents.Outbox.OutboxMessagesClaimed,
            messages.Count,
            LockDuration.TotalSeconds);

        var total = messages.Count;
        var succeeded = 0;
        var failed = 0;

        foreach (var message in messages)
        {
            using var messageScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                [LoggingProperties.OutboxMessageId] = message.Id,
                [LoggingProperties.MessageId] = message.EmailMessageId,
            });

            try
            {
                var email = JsonSerializer.Deserialize<EmailMessage>(
                    message.PayloadJson,
                    JsonSerializerOptionsDefaults.Default);

                if (email is null)
                {
                    failed++;

                    await _emailOutboxRepository.MarkFailedAsync(
                        message.Id,
                        message.ProcessingLockToken!.Value,
                        "Email payload was null.",
                        cancellationToken);

                    _logger.LogError(
                        "{EventName} | Email message was null.",
                        EmailLogEvents.Outbox.MissingPayload);

                    continue;
                }

                using var emailScope = _logger.BeginScope(new Dictionary<string, object?>
                {
                    [LoggingProperties.CorrelationId] = email.CorrelationId,
                    [LoggingProperties.CampaignId] = email.CampaignId,
                    [LoggingProperties.EmailId] = email.Id,
                    [LoggingProperties.EmailTemplateType] = email.TemplateType,
                });

                _logger.LogInformation(
                    "{EventName} | Publishing email message to queue.",
                    EmailLogEvents.Outbox.PublishingStarted);

                var response = await _sqs.SendMessageAsync(
                    new SendMessageRequest
                    {
                        QueueUrl = _sqsOptions.EmailQueueUrl,
                        MessageBody = message.PayloadJson,
                    },
                    cancellationToken);

                var published = await _emailOutboxRepository.MarkPublishedAsync(
                    message.Id,
                    message.ProcessingLockToken!.Value,
                    cancellationToken);

                if (!published)
                {
                    failed++;

                    _logger.LogWarning(
                        "{EventName} | Lost outbox lock after publishing. Duplicate SQS delivery possible.",
                        EmailLogEvents.Outbox.LockLost);

                    continue;
                }

                _logger.LogDebug(
                    "{EventName} | Outbox marked published and database updated. OutboxMessageId={OutboxMessageId}",
                    EmailLogEvents.Outbox.OutboxMarkedPublished,
                    message.Id);

                succeeded++;

                using var sqsScope = _logger.BeginScope(new Dictionary<string, object?>
                {
                    [LoggingProperties.SqsMessageId] = response.MessageId,
                });

                _logger.LogInformation(
                    "{EventName} | Published email to queue successfully.",
                    EmailLogEvents.Outbox.Published);
            }
            catch (JsonException ex)
            {
                failed++;

                await HandleFailureAsync(
                    message,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "{EventName} | Invalid email JSON.",
                    EmailLogEvents.Worker.InvalidPayload);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "{EventName} | Email processing cancelled.",
                    EmailLogEvents.Worker.ProcessingCancelled);

                throw;
            }
            catch (Exception ex)
            {
                failed++;

                await HandleFailureAsync(
                    message,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "{EventName} | Publishing email to queue failed.",
                    EmailLogEvents.Outbox.PublishFailed);
            }
        }

        return new EmailOutboxProcessResultModel
        {
            Retrieved = total,
            Succeeded = succeeded,
            Failed = failed,
        };
    }

/// <summary>
/// Handles outbox publishing failures and updates retry state.
/// </summary>
private async Task HandleFailureAsync(
    EmailOutboxEntity message,
    string error,
    CancellationToken cancellationToken)
    {
        var lockToken = message.ProcessingLockToken!.Value;

        message.RetryCount++;

        if (message.RetryCount >= MaxRetryCount)
        {
            await _emailOutboxRepository.MarkFailedAsync(
                message.Id,
                lockToken,
                error,
                cancellationToken);

            _logger.LogError(
                "{EventName} | Outbox message moved to terminal failure.",
                EmailLogEvents.Outbox.PublishFailed);

            return;
        }

        await _emailOutboxRepository.IncrementRetryAsync(
            message.Id,
            lockToken,
            error,
            cancellationToken);
    }
}