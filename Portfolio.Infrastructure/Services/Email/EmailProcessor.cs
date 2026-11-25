using Microsoft.Extensions.Logging;
using Portfolio.Application.Common.Exceptions.Email;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using System.Diagnostics;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Processes email messages through composition, throttling, provider delivery, and status tracking.
/// </summary>
public sealed class EmailProcessor : IEmailProcessor
{
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailComposer _composer;
    private readonly IEmailSender _sender;
    private readonly IEmailQuotaService _quotaService;
    private readonly IEmailRateLimiter _rateLimiter;
    private readonly IEmailProviderRateLimiter _providerRateLimiter;
    private readonly ILogger<EmailProcessor> _logger;

    private static readonly TimeSpan ProcessingLockDuration = TimeSpan.FromMinutes(5);

    public EmailProcessor(
        IEmailRepository emailRepository,
        IEmailComposer composer,
        IEmailSender sender,
        IEmailQuotaService quotaService,
        IEmailRateLimiter rateLimiter,
        IEmailProviderRateLimiter providerRateLimiter,
        ILogger<EmailProcessor> logger)
    {
        _emailRepository = emailRepository;
        _composer = composer;
        _sender = sender;
        _quotaService = quotaService;
        _rateLimiter = rateLimiter;
        _providerRateLimiter = providerRateLimiter;
        _logger = logger;
    }

    /// <summary>
    /// Processes an email message and updates its delivery status.
    /// </summary>
    public async Task<EmailProcessResultModel> ProcessAsync(
        EmailMessage email,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            [LoggingProperties.Operation] = LoggingOperations.ProcessEmail
        });

        var lockResult = await _emailRepository.TryAcquireProcessingLockAsync(
            email.Id,
            DateTime.UtcNow.Add(ProcessingLockDuration),
            cancellationToken);

        if (!lockResult.Acquired)
        {
            switch (lockResult.FailureReason)
            {
                case ProcessingLockFailureReason.AlreadyLocked:

                    _logger.LogInformation(
                        "{EventName} | Email already processing. ExistingLockUntilUtc={ExistingLockUntilUtc}",
                        EmailLogEvents.Processor.AlreadyProcessing,
                        lockResult.ExistingLockUntilUtc);

                    return new EmailProcessResultModel
                    {
                        Result = EmailProcessResult.Locked,
                        CorrelationId = email.CorrelationId
                    };

                case ProcessingLockFailureReason.AlreadyCompleted:

                    _logger.LogInformation(
                        "{EventName} | Email already completed.",
                        EmailLogEvents.Processor.AlreadyProcessed);

                    return new EmailProcessResultModel
                    {
                        Result = EmailProcessResult.AlreadyProcessed,
                        CorrelationId = email.CorrelationId
                    };

                case ProcessingLockFailureReason.NotFound:

                    _logger.LogInformation(
                        "{EventName} | Email record no longer exists. EmailId={EmailId}",
                        EmailLogEvents.Processor.NotFound,
                        email.Id);

                    return new EmailProcessResultModel
                    {
                        Result = EmailProcessResult.NotFound,
                        CorrelationId = email.CorrelationId
                    };
            }
        }

        _logger.LogInformation(
            "{EventName} | Email processing lock acquired. EmailId={EmailId}, LockDurationSeconds={LockDurationSeconds}",
            EmailLogEvents.Processor.LockAcquired,
            email.Id,
            ProcessingLockDuration.TotalSeconds);

        var lockToken = lockResult.Token
            ?? throw new InvalidOperationException(
                "Processing lock acquired without token.");

        try
        {
            var content = await _composer.ComposeAsync(
                email,
                cancellationToken);

            await _quotaService.CheckAsync(cancellationToken);
            await _rateLimiter.WaitAsync(cancellationToken);
            await _providerRateLimiter.WaitAsync(cancellationToken);

            var providerMessageId = await _sender.SendAsync(
                email,
                content.HtmlBody,
                content.TextBody,
                cancellationToken);

            var sent = await _emailRepository.MarkSentAsync(
                email.Id,
                lockToken,
                providerMessageId,
                cancellationToken);

            if (!sent)
            {
                throw new InvalidOperationException(
                    "Lost processing lock while marking email sent.");
            }

            _logger.LogInformation(
                "{EventName} | Email sent successfully. EmailId={EmailId}, ProviderMessageId={ProviderMessageId}",
                EmailLogEvents.Processor.Sent,
                email.Id,
                providerMessageId);

            return new EmailProcessResultModel
            {
                Result = EmailProcessResult.Sent,
                CorrelationId = email.CorrelationId,
                ProviderMessageId = providerMessageId
            };
        }
        catch (EmailTemplateNotFoundException ex)
        {
            var failed = await _emailRepository.MarkFailedAsync(
                email.Id,
                lockToken,
                ex.Message,
                cancellationToken);

            if (!failed)
            {
                _logger.LogWarning(
                    "{EventName} | Lost processing lock while marking email failed.",
                    EmailLogEvents.Processor.LockLost);
            }

            return new EmailProcessResultModel
            {
                Result = EmailProcessResult.PermanentFailure,
                CorrelationId = email.CorrelationId
            };
        }
        catch (InvalidTemplateModelException ex)
        {
            var failed = await _emailRepository.MarkFailedAsync(
                email.Id,
                lockToken,
                ex.Message,
                cancellationToken);

            if (!failed)
            {
                _logger.LogWarning(
                    "{EventName} | Lost processing lock while marking email failed.",
                    EmailLogEvents.Processor.LockLost);
            }

            return new EmailProcessResultModel
            {
                Result = EmailProcessResult.PermanentFailure,
                CorrelationId = email.CorrelationId
            };
        }
        catch (OperationCanceledException)
        {
            await _emailRepository.ReleaseProcessingLockAsync(
                email.Id,
                lockToken,
                CancellationToken.None);

            throw;
        }
        catch (HttpRequestException ex)
        {
            await RecordRetryFailureAsync(
                email,
                lockToken,
                ex,
                cancellationToken);

            return new EmailProcessResultModel
            {
                Result = EmailProcessResult.RetryableFailure,
                CorrelationId = email.CorrelationId
            };
        }
        catch (Exception ex)
        {
            await RecordRetryFailureAsync(
                email,
                lockToken,
                ex,
                cancellationToken);

            throw;
        }
        finally
        {
            _logger.LogInformation(
                "{EventName} | Email processing finished. DurationMs={DurationMs}",
                EmailLogEvents.Processor.Completed,
                stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Records a failed processing attempt and releases the processing lock.
    /// </summary>
    private async Task RecordRetryFailureAsync(
        EmailMessage email,
        Guid lockToken,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var recorded = await _emailRepository.RecordFailedAttemptAsync(
            email.Id,
            lockToken,
            exception.Message,
            cancellationToken);

        if (!recorded)
        {
            _logger.LogWarning(
                "{EventName} | Lost processing lock while recording retry failure.",
                EmailLogEvents.Processor.LockLost);
        }
    }
}