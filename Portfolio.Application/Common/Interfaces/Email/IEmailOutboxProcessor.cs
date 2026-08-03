using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Processes pending email outbox messages.
/// </summary>
public interface IEmailOutboxProcessor
{
    /// <summary>
    /// Processes pending email outbox messages.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The result of the outbox processing operation.</returns>
    Task<EmailOutboxProcessResultModel> ProcessAsync(CancellationToken cancellationToken);
}