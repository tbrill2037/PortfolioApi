using System.Threading.RateLimiting;
using Portfolio.Application.Common.Interfaces.Email;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Provides application-level email rate limiting to control outbound email throughput.
/// </summary>
public sealed class EmailRateLimiter : IEmailRateLimiter
{
    private readonly RateLimiter _limiter;

    public EmailRateLimiter()
    {
        _limiter =
            new TokenBucketRateLimiter(
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 7,
                    TokensPerPeriod = 7,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    AutoReplenishment = true,
                    QueueLimit = 1000,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
    }

    /// <summary>
    /// Waits until a rate limit token is available before allowing email processing to continue.
    /// </summary>
    public async Task WaitAsync(
        CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(1, cancellationToken);

        if (!lease.IsAcquired)
            throw new TimeoutException("Email rate limit exceeded.");
    }
}