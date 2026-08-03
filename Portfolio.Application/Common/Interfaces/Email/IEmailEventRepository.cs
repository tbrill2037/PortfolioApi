using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Provides persistence operations for email events.
/// </summary>
public interface IEmailEventRepository
{
    /// <summary>
    /// Persists an email event associated with an email message.
    /// </summary>
    /// <param name="email">The email message associated with the event.</param>
    /// <param name="emailEvent">The email event to record.</param>
    /// <param name="eventType">The type of email event.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddEventRecordAsync(
        EmailMessageEntity email,
        EmailEvent emailEvent,
        EmailEventType eventType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether an email event has already been recorded.
    /// </summary>
    /// <param name="sesMessageId">The SES message identifier.</param>
    /// <param name="eventType">The type of email event.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the event exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(string sesMessageId, EmailEventType eventType, CancellationToken cancellationToken);
}