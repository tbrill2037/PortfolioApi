using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Messaging;
using System.Text.Json;

namespace Portfolio.UnitTests.AWS.Messaging;

public sealed class SqsEmailQueuePublisherTests
{
    [Fact]
    public async Task PublishAsync_ShouldSendSerializedMessageToConfiguredQueue()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/PortfolioEmailQueue";
        const string messageId = "sqs-message-123";

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = queueUrl,
            OutboxQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue",
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        var message = new EmailMessage
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecipientEmail = new EmailAddress
            {
                Address = "recipient@example.com",
                DisplayName = "Recipient"
            },
            SenderEmail = new EmailAddress
            {
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        SendMessageRequest? capturedRequest = null;

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .Callback<SendMessageRequest, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
            })
            .ReturnsAsync(new SendMessageResponse
            {
                MessageId = messageId
            });

        var publisher = new SqsEmailQueuePublisher(
            sqs.Object,
            options);

        // Act
        var result = await publisher.PublishAsync(
            message,
            cancellationToken);

        // Assert
        result.Should().Be(messageId);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.QueueUrl.Should().Be(queueUrl);

        var deserializedMessage = JsonSerializer.Deserialize<EmailMessage>(
            capturedRequest.MessageBody,
            Portfolio.Application.Common.Serialization.JsonSerializerOptionsDefaults.Default);

        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.Id.Should().Be(message.Id);
        deserializedMessage.CorrelationId.Should().Be(message.CorrelationId);
        deserializedMessage.RecipientEmail.Address.Should().Be(message.RecipientEmail.Address);
        deserializedMessage.SenderEmail.Address.Should().Be(message.SenderEmail.Address);
        deserializedMessage.Subject.Should().Be(message.Subject);
        deserializedMessage.TemplateType.Should().Be(message.TemplateType);
        deserializedMessage.TemplatePath.Should().Be(message.TemplatePath);

        sqs.Verify(
            x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldPropagateSqsException()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailQueue";

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = queueUrl,
            OutboxQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue",
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        var message = new EmailMessage
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecipientEmail = new EmailAddress
            {
                Address = "recipient@example.com",
                DisplayName = "Recipient"
            },
            SenderEmail = new EmailAddress
            {
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var exception = new AmazonSQSException("SQS request failed.");

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .ThrowsAsync(exception);

        var publisher = new SqsEmailQueuePublisher(
            sqs.Object,
            options);

        // Act
        var act = () => publisher.PublishAsync(message, cancellationToken);

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
    public async Task PublishAsync_ShouldPropagateCancellation()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailQueue";

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = queueUrl,
            OutboxQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue",
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        var message = new EmailMessage
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecipientEmail = new EmailAddress
            {
                Address = "recipient@example.com",
                DisplayName = "Recipient"
            },
            SenderEmail = new EmailAddress
            {
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var exception = new OperationCanceledException("SQS request cancelled.");

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .ThrowsAsync(exception);

        var publisher = new SqsEmailQueuePublisher(
            sqs.Object,
            options);

        // Act
        var act = () => publisher.PublishAsync(message, cancellationToken);

        // Assert
        var thrownException = await act.Should()
            .ThrowAsync<OperationCanceledException>();

        thrownException.Which.Should().BeSameAs(exception);

        sqs.Verify(
            x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldForwardCancellationTokenToSqs()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailQueue";

        var sqs = new Mock<IAmazonSQS>();

        var options = Options.Create(new SqsOptions
        {
            EmailQueueUrl = queueUrl,
            OutboxQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxQueue",
            OutboxDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestOutboxDLQ",
            EmailDeadLetterQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailDLQ",
            EmailEventQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/TestEmailEventsQueue"
        });

        var message = new EmailMessage
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            RecipientEmail = new EmailAddress
            {
                Address = "recipient@example.com",
                DisplayName = "Recipient"
            },
            SenderEmail = new EmailAddress
            {
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        sqs.Setup(x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                cancellationToken))
            .ReturnsAsync(new SendMessageResponse
            {
                MessageId = "test-message-id"
            });

        var publisher = new SqsEmailQueuePublisher(
            sqs.Object,
            options);

        // Act
        var result = await publisher.PublishAsync(message, cancellationToken);

        // Assert
        result.Should().Be("test-message-id");

        sqs.Verify(
            x => x.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                It.Is<CancellationToken>(token => token == cancellationToken)),
            Times.Once);
    }
}