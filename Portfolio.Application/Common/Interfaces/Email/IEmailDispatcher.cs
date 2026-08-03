using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Queues email requests for asynchronous processing.
/// </summary>
public interface IEmailDispatcher
{
    /// <summary>
    /// Queues a Contact Me email for asynchronous processing.
    /// </summary>
    /// <param name="request">The Contact Me email request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task QueueContactMeEmailAsync(ContactMeRequestDTO request, CancellationToken cancellationToken = default);
}