using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Messaging;
using System.Text.Json;

namespace Portfolio.UnitTests.AWS.Messaging;

public sealed class SqsOutboxNotifierTests
{
    [Fact]
    public async Task NotifyAsync_ShouldSendOutboxNotificationToConfiguredQueue()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue";

        var emailId = Guid.NewGuid();

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailQueue",
            OutboxQueueUrl = queueUrl,
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        SendMessageRequest? capturedRequest = null;

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .Callback<SendMessageRequest, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
            })
            .ReturnsAsync(new SendMessageResponse());

        var notifier = new SqsOutboxNotifier(sqs.Object, options);

        // Act
        await notifier.NotifyAsync(emailId, cancellationToken);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.QueueUrl.Should().Be(queueUrl);

        using var document = JsonDocument.Parse(capturedRequest.MessageBody);

        document.RootElement.GetProperty("EmailId")
            .GetGuid()
            .Should().Be(emailId);

        document.RootElement.GetProperty("EventType")
            .GetString()
            .Should().Be("ProcessOutbox");

        sqs.Verify(
            x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_ShouldPropagateSqsException()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailQueue",
            OutboxQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue",
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        var emailId = Guid.NewGuid();
        var exception = new AmazonSQSException("SQS request failed.");

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .ThrowsAsync(exception);

        var notifier = new SqsOutboxNotifier(sqs.Object, options);

        // Act
        var act = () => notifier.NotifyAsync(emailId, cancellationToken);

        // Assert
        var thrownException = await act.Should()
            .ThrowAsync<AmazonSQSException>();

        thrownException.Which.Should().BeSameAs(exception);

        sqs.Verify(
            x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_ShouldForwardCancellationTokenToSqs()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailQueue",
            OutboxQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue",
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        var emailId = Guid.NewGuid();

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .ReturnsAsync(new SendMessageResponse());

        var notifier = new SqsOutboxNotifier(sqs.Object, options);

        // Act
        await notifier.NotifyAsync(emailId, cancellationToken);

        // Assert
        sqs.Verify(
            x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                It.Is<CancellationToken>(token => token == cancellationToken)),
            Times.Once);
    }
}