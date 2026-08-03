using Portfolio.Domain.Entities.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Provides persistence operations for archived dead-letter email messages.
/// </summary>
public interface IEmailDeadLetterRepository
{
    /// <summary>
    /// Persists a dead-letter email record.
    /// </summary>
    /// <param name="entity">The dead-letter entity to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(EmailDeadLetterEntity entity, CancellationToken cancellationToken);
}