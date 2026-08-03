using Portfolio.Domain.Entities;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Provides persistence operations for email outbox messages.
/// </summary>
public interface IEmailOutboxRepository
{
    /// <summary>
    /// Persists a new email outbox record.
    /// </summary>
    /// <param name="entity">The outbox entity to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(EmailOutboxEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Claims a batch of pending outbox messages for processing.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to claim.</param>
    /// <param name="lockUntilUtc">The time until the processing lock expires.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The claimed outbox messages.</returns>
    Task<List<EmailOutboxEntity>> ClaimPendingAsync(
        int batchSize,
        DateTime lockUntilUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an outbox message as successfully published.
    /// </summary>
    /// <param name="outboxId">The outbox message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> MarkPublishedAsync(
        Guid outboxId,
        Guid lockToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Increments the retry count for an outbox message.
    /// </summary>
    /// <param name="outboxId">The outbox message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="errorMessage">The error that caused the retry.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> IncrementRetryAsync(
        Guid outboxId,
        Guid lockToken,
        string errorMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an outbox message as permanently failed.
    /// </summary>
    /// <param name="outboxId">The outbox message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="errorMessage">The error that caused the failure.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> MarkFailedAsync(
        Guid outboxId,
        Guid lockToken,
        string errorMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the processing lock for an outbox message.
    /// </summary>
    /// <param name="outboxId">The outbox message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the lock was released; otherwise, <see langword="false"/>.</returns>
    Task<bool> ReleaseProcessingLockAsync(
        Guid outboxId,
        Guid lockToken,
        CancellationToken cancellationToken);
}