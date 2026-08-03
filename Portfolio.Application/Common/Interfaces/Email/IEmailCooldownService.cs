using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Enforces cooldown rules for outgoing email requests.
/// </summary>
public interface IEmailCooldownService
{
    /// <summary>
    /// Verifies that the specified email request is not currently subject to a cooldown period.
    /// </summary>
    /// <param name="context">The context used to evaluate the cooldown.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task CheckAsync(EmailCooldownContext context, CancellationToken cancellationToken);
}