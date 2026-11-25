using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Email;

/// <summary>
/// Provides persistence operations for email delivery events.
/// </summary>
public sealed class EmailEventRepository : IEmailEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailEventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddEventRecordAsync(
        EmailMessageEntity email,
        EmailEvent emailEvent,
        EmailEventType eventType,
        CancellationToken cancellationToken)
    {
        var entity = new EmailEventEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            ProviderMessageId = emailEvent.ProviderMessageId,
            TransportMessageId = emailEvent.TransportMessageId,
            TransportTopic = emailEvent.TransportTopic,
            EventType = eventType,
            PayloadJson = emailEvent.RawPayload,
            ReceivedUtc = emailEvent.ReceivedUtc,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.EmailEvents.AddAsync(entity, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        string providerMessageId,
        EmailEventType eventType,
        CancellationToken cancellationToken)
    {
        return _dbContext.EmailEvents.AnyAsync(
            x => x.ProviderMessageId == providerMessageId && x.EventType == eventType,
            cancellationToken);
    }
}