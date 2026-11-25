using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Provides persistence operations for email message lifecycle management.
/// </summary>
public interface IEmailRepository
{
    /// <summary>
    /// Persists a new email message.
    /// </summary>
    /// <param name="email">The email message to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(EmailMessageEntity email, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an email message by its provider message identifier.
    /// </summary>
    /// <param name="providerMessageId">The email provider's message identifier.</param>
    Task<EmailMessageEntity?> GetByProviderMessageIdAsync(
        string providerMessageId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to acquire a processing lock for an email message.
    /// </summary>
    /// <param name="emailId">The email message identifier.</param>
    /// <param name="lockUntilUtc">The time until the processing lock expires.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The result of the lock acquisition attempt.</returns>
    Task<ProcessingLockResult> TryAcquireProcessingLockAsync(
        Guid emailId,
        DateTime lockUntilUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases the processing lock for an email message.
    /// </summary>
    /// <param name="emailId">The email message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task ReleaseProcessingLockAsync(
        Guid emailId,
        Guid lockToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an email message as successfully sent.
    /// </summary>
    /// <param name="emailId">The email message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="providerMessageId">The email provider's message identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> MarkSentAsync(
        Guid emailId,
        Guid lockToken,
        string providerMessageId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an email message as failed.
    /// </summary>
    /// <param name="emailId">The email message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="errorMessage">The error that caused the failure.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> MarkFailedAsync(
        Guid emailId,
        Guid lockToken,
        string errorMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an email message as dead-lettered.
    /// </summary>
    /// <param name="emailId">The email message identifier.</param>
    /// <param name="errorMessage">The error that caused the message to be dead-lettered.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> MarkDeadLetteredAsync(
        Guid emailId,
        string errorMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records a failed processing attempt for an email message.
    /// </summary>
    /// <param name="emailId">The email message identifier.</param>
    /// <param name="lockToken">The processing lock token.</param>
    /// <param name="errorMessage">The error that caused the failure.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the update succeeded; otherwise, <see langword="false"/>.</returns>
    Task<bool> RecordFailedAttemptAsync(
        Guid emailId,
        Guid lockToken,
        string errorMessage,
        CancellationToken cancellationToken);
}