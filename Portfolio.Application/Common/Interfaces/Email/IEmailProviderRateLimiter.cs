namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Enforces the configured email provider sending rate.
/// </summary>
public interface IEmailProviderRateLimiter
{
    /// <summary>
    /// Waits until the email provider permits another send operation.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task WaitAsync(CancellationToken cancellationToken);
}