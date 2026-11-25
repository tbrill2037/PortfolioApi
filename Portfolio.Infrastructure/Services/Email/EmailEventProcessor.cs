using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Processes email provider lifecycle events and updates email status records.
/// </summary>
public sealed class EmailEventProcessor : IEmailEventProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailRepository _emailRepository;
    private readonly IEmailEventRepository _eventRepository;
    private readonly ILogger<EmailEventProcessor> _logger;

    public EmailEventProcessor(
        ApplicationDbContext dbContext,
        IEmailRepository emailRepository,
        IEmailEventRepository eventRepository,
        ILogger<EmailEventProcessor> logger)
    {
        _dbContext = dbContext;
        _emailRepository = emailRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <summary>
    /// Processes an email provider event and updates the related email lifecycle state.
    /// </summary>
    public async Task ProcessAsync(
        EmailEvent emailEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(emailEvent);

        var providerMessageId = emailEvent.ProviderMessageId;
        var eventType = emailEvent.EventType;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Operation] = LoggingOperations.ProcessEmailEvent,
            [LoggingProperties.ProviderMessageId] = providerMessageId,
            [LoggingProperties.EmailEventType] = eventType,
            [LoggingProperties.ProviderReceivedUtc] = emailEvent.ReceivedUtc
        });

        var alreadyProcessed = await _eventRepository.ExistsAsync(
            providerMessageId,
            eventType,
            cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "{EventName} | Provider event already processed.",
                EmailLogEvents.Provider.EventAlreadyProcessed);

            return;
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var email = await _emailRepository.GetByProviderMessageIdAsync(
                    providerMessageId,
                    cancellationToken);

                if (email is null)
                {
                    _logger.LogWarning(
                        "{EventName} | Email record not found. Provider event ignored.",
                        EmailLogEvents.Provider.EventEmailMissing);

                    return;
                }

                using var emailScope = _logger.BeginScope(new Dictionary<string, object?>
                {
                    [LoggingProperties.EmailId] = email.Id,
                    [LoggingProperties.CorrelationId] = email.CorrelationId,
                    [LoggingProperties.CampaignId] = email.CampaignId,
                    [LoggingProperties.EmailTemplateType] = email.TemplateType
                });

                await _eventRepository.AddEventRecordAsync(
                    email,
                    emailEvent,
                    eventType,
                    cancellationToken);

                var previousStatus = email.Status;

                ApplyStatusTransition(email, eventType);

                if (previousStatus != email.Status)
                {
                    _logger.LogInformation(
                        "{EventName} | Email status updated. Previous={PreviousStatus}, Current={CurrentStatus}",
                        EmailLogEvents.Provider.StatusUpdated,
                        previousStatus,
                        email.Status);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "{EventName} | Provider event processed.",
                    EmailLogEvents.Provider.EventProcessed);
            }
            catch (DbUpdateException ex)
                when (IsDuplicateKeyViolation(ex))
            {
                _logger.LogInformation(
                    "{EventName} | Duplicate provider event ignored. ProviderMessageId={ProviderMessageId}, EventType={EventType}",
                    EmailLogEvents.Provider.EventAlreadyProcessed,
                    providerMessageId,
                    eventType);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(
                    ex,
                    "{EventName} | Provider processing cancelled.",
                    EmailLogEvents.Provider.EventCancelled);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{EventName} | Failed processing provider event.",
                    EmailLogEvents.Provider.EventFailed);

                throw;
            }
        });
    }

    private static void ApplyStatusTransition(
        EmailMessageEntity email,
        EmailEventType eventType)
    {
        if (!EmailStatusTransitionValidator.CanTransition(email.Status, eventType))
        {
            return;
        }

        email.Status = eventType switch
        {
            EmailEventType.Send => EmailStatus.Sent,
            EmailEventType.Delivery => EmailStatus.Delivered,
            EmailEventType.Bounce => EmailStatus.Bounced,
            EmailEventType.Complaint => EmailStatus.Complained,
            EmailEventType.Reject => EmailStatus.Rejected,
            EmailEventType.RenderingFailure => EmailStatus.Failed,
            EmailEventType.Open => EmailStatus.Opened,
            EmailEventType.Click => EmailStatus.Clicked,
            EmailEventType.DeliveryDelay => EmailStatus.DeliveryDelay,
            EmailEventType.Subscription => EmailStatus.Subscription,
            _ => email.Status
        };
    }

    private static bool IsDuplicateKeyViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}