
namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Notifies the email outbox processor that new messages are available.
/// </summary>
public interface IEmailOutboxNotifier
{
    /// <summary>
    /// Notifies the outbox processor that an email has been queued.
    /// </summary>
    /// <param name="emailId">The identifier of the queued email.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task NotifyAsync(
        Guid emailId,
        CancellationToken cancellationToken = default);
}
