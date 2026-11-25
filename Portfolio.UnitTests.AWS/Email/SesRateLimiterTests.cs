using FluentAssertions;
using Microsoft.Extensions.Options;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Services.Email;

namespace Portfolio.UnitTests.AWS.Email;

public sealed class SesRateLimiterTests
{
    [Fact]
    public async Task WaitAsync_ShouldCompleteWhenTokenIsAvailable()
    {
        // Arrange
        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10,
            MaxEmailsPerSecond = 1,
            RateLimitQueueLimit = 0
        });

        var limiter = new SesRateLimiter(options);

        // Act
        var act = async () => await limiter.WaitAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitAsync_ShouldThrowWhenRateLimitIsExceeded()
    {
        // Arrange
        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10,
            MaxEmailsPerSecond = 1,
            RateLimitQueueLimit = 0
        });

        var limiter = new SesRateLimiter(options);

        await limiter.WaitAsync(TestContext.Current.CancellationToken);

        // Act
        var act = async () => await limiter.WaitAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<TimeoutException>()
            .WithMessage("SES rate limit exceeded.");
    }

    [Fact]
    public async Task WaitAsync_ShouldPropagateCancellation()
    {
        // Arrange
        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10,
            MaxEmailsPerSecond = 1,
            RateLimitQueueLimit = 1
        });

        var limiter = new SesRateLimiter(options);

        await limiter.WaitAsync(TestContext.Current.CancellationToken);

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        var act = async () => await limiter.WaitAsync(
            cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WaitAsync_ShouldAllowQueuedRequestAfterTokenReplenishment()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10,
            MaxEmailsPerSecond = 1,
            RateLimitQueueLimit = 1
        });

        var limiter = new SesRateLimiter(options);

        await limiter.WaitAsync(cancellationToken);

        // Act
        var queuedRequest = limiter.WaitAsync(cancellationToken);

        // Assert
        await queuedRequest.WaitAsync(
            TimeSpan.FromSeconds(3),
            cancellationToken);
    }
}