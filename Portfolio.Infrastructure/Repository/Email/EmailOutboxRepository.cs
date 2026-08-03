using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Domain.Entities;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Email;

/// <summary>
/// Provides persistence operations for email outbox processing.
/// </summary>
public sealed class EmailOutboxRepository : IEmailOutboxRepository
{
    private readonly ApplicationDbContext _context;

    public EmailOutboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailOutboxEntity entity, CancellationToken cancellationToken)
    {
        await _context.EmailOutboxes.AddAsync(entity, cancellationToken);
    }

    public async Task<List<EmailOutboxEntity>> ClaimPendingAsync(
        int batchSize,
        DateTime lockUntilUtc,
        CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var messages = await _context.EmailOutboxes
                .FromSqlInterpolated($@"
                    SELECT TOP ({batchSize}) *
                    FROM EmailOutboxes WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE ProcessedUtc IS NULL
                    AND (
                        ProcessingLockUntilUtc IS NULL
                        OR ProcessingLockUntilUtc < {now}
                    )
                    ORDER BY CreatedUtc")
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                message.ProcessingLockUntilUtc = lockUntilUtc;
                message.ProcessingLockToken = Guid.NewGuid();
                message.ProcessingUtc = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return messages;
        });
    }

    public async Task<bool> MarkPublishedAsync(Guid outboxId, Guid lockToken, CancellationToken cancellationToken)
    {
        var rows = await _context.EmailOutboxes
            .Where(x => x.Id == outboxId)
            .Where(x => x.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ProcessedUtc, DateTime.UtcNow)
                    .SetProperty(x => x.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> ReleaseProcessingLockAsync(Guid outboxId, Guid lockToken, CancellationToken cancellationToken)
    {
        var rows = await _context.EmailOutboxes
            .Where(x => x.Id == outboxId)
            .Where(x => x.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> MarkFailedAsync(Guid outboxId, Guid lockToken, string errorMessage, CancellationToken cancellationToken)
    {
        var rows = await _context.EmailOutboxes
            .Where(x => x.Id == outboxId)
            .Where(x => x.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ErrorMessage, errorMessage)
                    .SetProperty(x => x.ProcessedUtc, DateTime.UtcNow)
                    .SetProperty(x => x.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }

    public async Task<bool> IncrementRetryAsync(Guid outboxId, Guid lockToken, string errorMessage, CancellationToken cancellationToken)
    {
        var rows = await _context.EmailOutboxes
            .Where(x => x.Id == outboxId)
            .Where(x => x.ProcessingLockToken == lockToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RetryCount, x => x.RetryCount + 1)
                    .SetProperty(x => x.ErrorMessage, errorMessage)
                    .SetProperty(x => x.ProcessingLockUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.ProcessingLockToken, (Guid?)null),
                cancellationToken);

        return rows == 1;
    }
}