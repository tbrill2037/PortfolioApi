using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Processes email events received from the email provider.
/// </summary>
public interface IEmailEventProcessor
{
    /// <summary>
    /// Processes an email event.
    /// </summary>
    /// <param name="emailEvent">The email event to process.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task ProcessAsync(EmailEvent emailEvent, CancellationToken cancellationToken);
}