using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Services.Email;

namespace Portfolio.UnitTests.AWS.Email;

public sealed class SesQuotaServiceTests
{
    [Fact]
    public async Task CheckAsync_ShouldCompleteWhenQuotaIsAvailable()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAccountResponse
            {
                SendQuota = new SendQuota
                {
                    Max24HourSend = 100,
                    SentLast24Hours = 40
                }
            });

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = Mock.Of<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger);

        // Act
        var act = async () => await service.CheckAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();

        ses.Verify(x => x.GetAccountAsync(
            It.IsAny<GetAccountRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckAsync_ShouldThrowWhenQuotaIsExhausted()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAccountResponse
            {
                SendQuota = new SendQuota
                {
                    Max24HourSend = 100,
                    SentLast24Hours = 100
                }
            });

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = Mock.Of<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger);

        // Act
        var act = async () => await service.CheckAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SES daily sending quota exhausted.");
    }

    [Fact]
    public async Task CheckAsync_ShouldContinueWhenQuotaIsNull()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAccountResponse
            {
                SendQuota = null
            });

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = Mock.Of<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger);

        // Act
        var act = async () => await service.CheckAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CheckAsync_ShouldContinueWhenQuotaRetrievalFails()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSimpleEmailServiceV2Exception("SES unavailable"));

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = Mock.Of<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger);

        // Act
        var act = async () => await service.CheckAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CheckAsync_ShouldPropagateCancellation()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        var cancellationToken = new CancellationToken(true);

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = Mock.Of<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger);

        // Act
        var act = async () => await service.CheckAsync(cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        ses.Verify(x => x.GetAccountAsync(
            It.IsAny<GetAccountRequest>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CheckAsync_ShouldNotWarnWhenRemainingQuotaEqualsThreshold()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAccountResponse
            {
                SendQuota = new SendQuota
                {
                    Max24HourSend = 100,
                    SentLast24Hours = 90
                }
            });

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = new Mock<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger.Object);

        // Act
        await service.CheckAsync(TestContext.Current.CancellationToken);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("SES quota nearly exhausted.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAsync_ShouldContinueWhenQuotaIsLow()
    {
        // Arrange
        var ses = new Mock<IAmazonSimpleEmailServiceV2>();

        ses.Setup(x => x.GetAccountAsync(
                It.IsAny<GetAccountRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetAccountResponse
            {
                SendQuota = new SendQuota
                {
                    Max24HourSend = 100,
                    SentLast24Hours = 95
                }
            });

        var options = Options.Create(new SesOptions
        {
            ConfigurationSet = "TestConfigurationSet",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            LowQuotaThreshold = 10
        });

        var logger = new Mock<ILogger<SesQuotaService>>();

        var service = new SesQuotaService(ses.Object, options, logger.Object);

        // Act
        var act = async () => await service.CheckAsync(
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("SES quota nearly exhausted.")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}