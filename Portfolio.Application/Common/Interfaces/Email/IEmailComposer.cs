using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Composes rendered email content from an email message and its associated template.
/// </summary>
public interface IEmailComposer
{
    /// <summary>
    /// Composes the HTML and plain-text content for an email message.
    /// </summary>
    /// <param name="message">The email message to compose.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The composed email content.</returns>
    Task<EmailContent> ComposeAsync(EmailMessage message, CancellationToken cancellationToken = default);
}