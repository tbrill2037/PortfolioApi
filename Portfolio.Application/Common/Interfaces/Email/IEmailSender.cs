using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Sends composed email messages.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="message">The email message metadata.</param>
    /// <param name="htmlBody">The rendered HTML email body.</param>
    /// <param name="textBody">The rendered plain-text email body.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The email provider's message identifier.</returns>
    Task<string> SendAsync(EmailMessage message, string htmlBody, string textBody, CancellationToken cancellationToken = default);
}