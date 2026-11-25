using FluentAssertions;
using Microsoft.Extensions.Options;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Services.Lambda;
using Portfolio.Infra.Configuration;

namespace Portfolio.UnitTests.AWS.Lambda;

public sealed class LambdaExecutionTimeoutProviderTests
{
    [Fact]
    public void CreateTimeout_ShouldSubtractBufferFromRemainingTime()
    {
        // Arrange
        var lambdaOptions = Options.Create(new LambdaOptions
        {
            DevelopmentFallbackTime = "00:05:00",
            EmailOutboxProcessingBuffer = "00:00:10",
            EmailProcessingBuffer = "00:00:10",
            EventProcessingBuffer = "00:00:10",
            DlqProcessingBuffer = "00:00:10"
        });

        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            IsDevelopment = false
        });

        var provider = new LambdaExecutionTimeoutProvider(
            lambdaOptions,
            environmentOptions);

        // Act
        using var result = provider.CreateTimeout(
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(10));

        // Assert
        result.Should().NotBeNull();
        result!.Timeout.Should().Be(TimeSpan.FromSeconds(50));
        result.Token.Should().NotBe(CancellationToken.None);
        result.Token.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void CreateTimeout_ShouldReturnNullWhenRemainingTimeIsLessThanBuffer()
    {
        // Arrange
        var lambdaOptions = Options.Create(new LambdaOptions
        {
            DevelopmentFallbackTime = "00:05:00",
            EmailOutboxProcessingBuffer = "00:00:10",
            EmailProcessingBuffer = "00:00:10",
            EventProcessingBuffer = "00:00:10",
            DlqProcessingBuffer = "00:00:10"
        });

        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            IsDevelopment = false
        });

        var provider = new LambdaExecutionTimeoutProvider(
            lambdaOptions,
            environmentOptions);

        // Act
        var result = provider.CreateTimeout(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreateTimeout_ShouldReturnNullWhenRemainingTimeEqualsBuffer()
    {
        // Arrange
        var lambdaOptions = Options.Create(new LambdaOptions
        {
            DevelopmentFallbackTime = "00:05:00",
            EmailOutboxProcessingBuffer = "00:00:10",
            EmailProcessingBuffer = "00:00:10",
            EventProcessingBuffer = "00:00:10",
            DlqProcessingBuffer = "00:00:10"
        });

        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            IsDevelopment = false
        });

        var provider = new LambdaExecutionTimeoutProvider(
            lambdaOptions,
            environmentOptions);

        // Act
        var result = provider.CreateTimeout(
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreateTimeout_ShouldUseDevelopmentFallbackWhenInDevelopment()
    {
        // Arrange
        var lambdaOptions = Options.Create(new LambdaOptions
        {
            DevelopmentFallbackTime = "00:02:00",
            EmailOutboxProcessingBuffer = "00:00:10",
            EmailProcessingBuffer = "00:00:10",
            EventProcessingBuffer = "00:00:10",
            DlqProcessingBuffer = "00:00:10"
        });

        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            IsDevelopment = true
        });

        var provider = new LambdaExecutionTimeoutProvider(
            lambdaOptions,
            environmentOptions);

        // Act
        using var result = provider.CreateTimeout(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10));

        // Assert
        result.Should().NotBeNull();
        result!.Timeout.Should().Be(TimeSpan.FromSeconds(110));
        result.Token.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void CreateTimeout_ShouldReturnNullWhenDevelopmentFallbackIsLessThanBuffer()
    {
        // Arrange
        var lambdaOptions = Options.Create(new LambdaOptions
        {
            DevelopmentFallbackTime = "00:00:05",
            EmailOutboxProcessingBuffer = "00:00:10",
            EmailProcessingBuffer = "00:00:10",
            EventProcessingBuffer = "00:00:10",
            DlqProcessingBuffer = "00:00:10"
        });

        var environmentOptions = Options.Create(new EnvironmentOptions
        {
            IsDevelopment = true
        });

        var provider = new LambdaExecutionTimeoutProvider(
            lambdaOptions,
            environmentOptions);

        // Act
        var result = provider.CreateTimeout(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(10));

        // Assert
        result.Should().BeNull();
    }
}