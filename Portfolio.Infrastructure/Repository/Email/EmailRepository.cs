using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Email;

/// <summary>
/// Provides persistence operations for email message processing and lifecycle updates.
/// </summary>
public sealed class EmailRepository : IEmailRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailMessageEntity email, CancellationToken cancellationToken)
    {
        await _dbContext.EmailMessages.AddAsync(email, cancellationToken);
    }

    public async Task<EmailMessageEntity?> GetBySesMessageIdAsync(string sesMessageId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmailMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SesMessageId == sesMessageId, cancellationToken);
    }

    public async Task<ProcessingLockResult> TryAcquireProcessingLockAsync(
        Guid emailId,
        DateTime lockUntil,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var lockToken = Guid.NewGuid();

        var rows = await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .Where(e => e.ProcessingLockUntilUtc == null || e.ProcessingLockUntilUtc < now)
            .Where(e => e.Status != EmailStatus.Sent && e.Status != EmailStatus.Failed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.ProcessingLockUntilUtc, lockUntil)
                    .SetProperty(e => e.ProcessingLockToken, lockToken)
                    .SetProperty(e => e.ProcessingUtc, now),
                cancellationToken);

        if (rows == 1)
        {
            return new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None);
        }

        var existing = await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .Select(e => new
            {
                e.ProcessingLockUntilUtc,
                e.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            return new ProcessingLockResult(
                false,
                null,
                null,
                ProcessingLockFailureReason.NotFound);
        }

        if (existing.Status is EmailStatus.Sent or EmailStatus.Failed)
        {
            return new ProcessingLockResult(
                false,
                null,
                null,
                ProcessingLockFailureReason.AlreadyCompleted);
        }

        return new ProcessingLockResult(
            false,
            null,
            existing.ProcessingLockUntilUtc,
            ProcessingLockFailureReason.AlreadyLocked);
    }

    public async Task ReleaseProcessingLockAsync(Guid emailId, Guid lockToken, CancellationToken cancellationToken)
    {
        await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .Where(e => e.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockToken, (Guid?)null),
                cancellationToken);
    }

    public async Task<bool> MarkSentAsync(Guid emailId, Guid lockToken, string sesMessageId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var rows = await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .Where(e => e.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.Status, EmailStatus.Sent)
                    .SetProperty(e => e.SesMessageId, sesMessageId)
                    .SetProperty(e => e.SentUtc, now)
                    .SetProperty(e => e.ErrorMessage, (string?)null)
                    .SetProperty(e => e.LastErrorUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> MarkFailedAsync(Guid emailId, Guid lockToken, string errorMessage, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var rows = await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .Where(e => e.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.Status, EmailStatus.Failed)
                    .SetProperty(e => e.ErrorMessage, errorMessage)
                    .SetProperty(e => e.LastErrorUtc, now)
                    .SetProperty(e => e.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> MarkDeadLetteredAsync(Guid emailId, string errorMessage, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var rows = await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.Status, EmailStatus.Failed)
                    .SetProperty(e => e.LastAttemptUtc, now)
                    .SetProperty(e => e.LastErrorUtc, now)
                    .SetProperty(e => e.ErrorMessage, errorMessage)
                    .SetProperty(e => e.DeadLetteredUtc, now)
                    .SetProperty(e => e.ProcessingUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> RecordFailedAttemptAsync(Guid emailId, Guid lockToken, string errorMessage, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var rows = await _dbContext.EmailMessages
            .Where(e => e.Id == emailId)
            .Where(e => e.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.RetryCount, e => e.RetryCount + 1)
                    .SetProperty(e => e.LastAttemptUtc, now)
                    .SetProperty(e => e.LastErrorUtc, now)
                    .SetProperty(e => e.ErrorMessage, errorMessage)
                    .SetProperty(e => e.ProcessingUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(e => e.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }
}