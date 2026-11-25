using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Processes email messages for delivery.
/// </summary>
public interface IEmailProcessor
{
    /// <summary>
    /// Processes an email message for delivery.
    /// </summary>
    /// <param name="email">The email message to process.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The result of the email processing operation.</returns>
    Task<EmailProcessResultModel> ProcessAsync(EmailMessage email, CancellationToken cancellationToken);
}