namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Enforces the configured Amazon SES sending rate.
/// </summary>
public interface ISesRateLimiter
{
    /// <summary>
    /// Waits until another request can be sent to Amazon SES without exceeding the configured rate limit.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task WaitAsync(CancellationToken cancellationToken);
}