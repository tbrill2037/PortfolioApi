using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Infra.AWS.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace Portfolio.Infra.AWS.Services.Email;

/// <summary>
/// Provides rate limiting for Amazon SES send operations.
/// </summary>
public sealed class SesRateLimiter : IEmailProviderRateLimiter
{
    private readonly RateLimiter _limiter;

    public SesRateLimiter(IOptions<SesOptions> options)
    {
        _limiter = new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = options.Value.MaxEmailsPerSecond,
                TokensPerPeriod = options.Value.MaxEmailsPerSecond,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                AutoReplenishment = true,
                QueueLimit = options.Value.RateLimitQueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }

    /// <summary>
    /// Waits until an SES send operation is permitted.
    /// </summary>
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(1, cancellationToken);

        if (!lease.IsAcquired)
            throw new TimeoutException("SES rate limit exceeded.");
    }
}