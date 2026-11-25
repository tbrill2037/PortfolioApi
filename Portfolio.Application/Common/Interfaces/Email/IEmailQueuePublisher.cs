
using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Publishes email messages to a provider-managed queue.
/// </summary>
public interface IEmailQueuePublisher
{
    /// <summary>
    /// Publishes an email message to the queue.
    /// </summary>
    /// <param name="message">The email message to publish.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The provider's queue message identifier.</returns>
    Task<string> PublishAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
