namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Enforces the configured email sending rate.
/// </summary>
public interface IEmailRateLimiter
{
    /// <summary>
    /// Waits until another email can be sent without exceeding the configured rate limit.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task WaitAsync(CancellationToken cancellationToken);
}