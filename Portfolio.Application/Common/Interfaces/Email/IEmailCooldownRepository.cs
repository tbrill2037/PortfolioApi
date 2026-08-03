using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Provides persistence operations for tracking email request cooldowns.
/// </summary>
public interface IEmailCooldownRepository
{
    /// <summary>
    /// Retrieves the most recent email request matching the specified cooldown context.
    /// </summary>
    /// <param name="context">The cooldown context used to identify prior requests.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The most recent matching email request, or <see langword="null"/> if none exists.</returns>
    Task<EmailRequestEntity?> GetLastRequestAsync(EmailCooldownContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new email request for cooldown tracking.
    /// </summary>
    /// <param name="request">The email request to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(EmailRequestEntity request, CancellationToken cancellationToken);
}