using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Queues email messages for asynchronous processing.
/// </summary>
public interface IEmailQueue
{
    /// <summary>
    /// Queues an email message for asynchronous processing.
    /// </summary>
    /// <param name="message">The email message to enqueue.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}