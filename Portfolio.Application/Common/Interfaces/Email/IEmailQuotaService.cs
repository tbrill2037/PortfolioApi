namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Verifies that email sending is within the configured quota limits.
/// </summary>
public interface IEmailQuotaService
{
    /// <summary>
    /// Verifies that email sending can continue without exceeding the configured quota.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task CheckAsync(CancellationToken cancellationToken);
}