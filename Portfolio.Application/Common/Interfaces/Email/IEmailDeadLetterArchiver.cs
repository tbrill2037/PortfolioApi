using Amazon.Lambda.SQSEvents;
using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Archives email messages that have been moved to the dead-letter queue.
/// </summary>
public interface IEmailDeadLetterArchiver
{
    /// <summary>
    /// Archives a dead-lettered email message and any associated processing details.
    /// </summary>
    /// <param name="message">The SQS message received from the dead-letter queue.</param>
    /// <param name="email">The associated email message, if available.</param>
    /// <param name="exception">The exception that caused processing to fail, if available.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task ArchiveAsync(SQSEvent.SQSMessage message, EmailMessage? email, Exception? exception, CancellationToken cancellationToken);
}