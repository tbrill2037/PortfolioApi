using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Email.Configuration;
using Portfolio.Infra.Services.Email;
using System.Text.Json;
using Xunit;

namespace Portfolio.UnitTests.Email;

public sealed class EmailOutboxProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldPublishMessageAndMarkOutboxAsPublished()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var email = new EmailMessage
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            PayloadJson = JsonSerializer.Serialize(email),
            CreatedUtc = DateTime.UtcNow,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        outboxRepository
            .Setup(x => x.MarkPublishedAsync(
                outbox.Id,
                lockToken,
                cancellationToken))
            .ReturnsAsync(true);

        queuePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                cancellationToken))
            .ReturnsAsync("queue-message-123");

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(cancellationToken);

        // Assert
        result.Retrieved.Should().Be(1);
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(0);

        queuePublisher.Verify(
            x => x.PublishAsync(
                It.Is<EmailMessage>(m => m.Id == email.Id),
                cancellationToken),
            Times.Once);

        outboxRepository.Verify(
            x => x.MarkPublishedAsync(
                outbox.Id,
                lockToken,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIncrementRetryWhenPublishingFails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var email = new EmailMessage
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            PayloadJson = JsonSerializer.Serialize(email),
            CreatedUtc = DateTime.UtcNow,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        queuePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable."));

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(cancellationToken);

        // Assert
        result.Retrieved.Should().Be(1);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(1);

        outboxRepository.Verify(
            x => x.IncrementRetryAsync(
                outbox.Id,
                lockToken,
                "Queue unavailable.",
                cancellationToken),
            Times.Once);

        outboxRepository.Verify(
            x => x.MarkPublishedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMarkFailedWhenMaxRetriesReached()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var email = new EmailMessage
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            PayloadJson = JsonSerializer.Serialize(email),
            CreatedUtc = DateTime.UtcNow,
            RetryCount = 2,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        queuePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable."));

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(cancellationToken);

        // Assert
        result.Retrieved.Should().Be(1);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(1);

        outboxRepository.Verify(
            x => x.MarkFailedAsync(
                outbox.Id,
                lockToken,
                "Queue unavailable.",
                cancellationToken),
            Times.Once);

        outboxRepository.Verify(
            x => x.IncrementRetryAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMarkFailedWhenPayloadIsNull()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = Guid.NewGuid(),
            PayloadJson = "null",
            CreatedUtc = DateTime.UtcNow,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(cancellationToken);

        // Assert
        result.Retrieved.Should().Be(1);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(1);

        outboxRepository.Verify(
            x => x.MarkFailedAsync(
                outbox.Id,
                lockToken,
                "Email payload was null.",
                cancellationToken),
            Times.Once);

        queuePublisher.Verify(
            x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIncrementRetryWhenPayloadIsInvalidJson()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = Guid.NewGuid(),
            PayloadJson = "{",
            CreatedUtc = DateTime.UtcNow,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(cancellationToken);

        // Assert
        result.Retrieved.Should().Be(1);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(1);

        outboxRepository.Verify(
            x => x.IncrementRetryAsync(
                outbox.Id,
                lockToken,
                It.Is<string>(error => !string.IsNullOrWhiteSpace(error)),
                cancellationToken),
            Times.Once);

        queuePublisher.Verify(
            x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReportFailureWhenProcessingLockIsLost()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var email = new EmailMessage
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            PayloadJson = JsonSerializer.Serialize(email),
            CreatedUtc = DateTime.UtcNow,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        queuePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                cancellationToken))
            .ReturnsAsync("queue-message-123");

        outboxRepository
            .Setup(x => x.MarkPublishedAsync(
                outbox.Id,
                lockToken,
                cancellationToken))
            .ReturnsAsync(false);

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(cancellationToken);

        // Assert
        result.Retrieved.Should().Be(1);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(1);

        queuePublisher.Verify(
            x => x.PublishAsync(
                It.Is<EmailMessage>(m => m.Id == email.Id),
                cancellationToken),
            Times.Once);

        outboxRepository.Verify(
            x => x.MarkPublishedAsync(outbox.Id, lockToken, cancellationToken),
            Times.Once);

        outboxRepository.Verify(
            x => x.IncrementRetryAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        outboxRepository.Verify(
            x => x.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldPropagateCancellationDuringPublishing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        var email = new EmailMessage
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var outbox = new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            PayloadJson = JsonSerializer.Serialize(email),
            CreatedUtc = DateTime.UtcNow,
            ProcessingLockToken = lockToken
        };

        var outboxRepository = new Mock<IEmailOutboxRepository>();
        var queuePublisher = new Mock<IEmailQueuePublisher>();

        outboxRepository
            .Setup(x => x.ClaimPendingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new List<EmailOutboxEntity> { outbox });

        queuePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<EmailMessage>(),
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        var options = Options.Create(new EmailProcessingOptions
        {
            ApplicationMaxEmailsPerSecond = 10,
            ApplicationRateLimitQueueLimit = 100,
            MaxOutboxRetries = 3,
            MaxEmailRetries = 3,
            MaxDLQRetries = 3,
            MaxEventRetries = 3
        });

        var processor = new EmailOutboxProcessor(
            outboxRepository.Object,
            queuePublisher.Object,
            options,
            NullLogger<EmailOutboxProcessor>.Instance);

        // Act
        var act = () => processor.ProcessAsync(cancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        outboxRepository.Verify(
            x => x.IncrementRetryAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        outboxRepository.Verify(
            x => x.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        outboxRepository.Verify(
            x => x.MarkPublishedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}