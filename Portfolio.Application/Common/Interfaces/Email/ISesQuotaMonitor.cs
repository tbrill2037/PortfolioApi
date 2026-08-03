namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Monitors the availability of the Amazon SES sending quota.
/// </summary>
public interface ISesQuotaMonitor
{
    /// <summary>
    /// Verifies that the Amazon SES sending quota is sufficient.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task CheckAsync(CancellationToken cancellationToken);
}