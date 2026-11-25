using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Repository.Email;
using Portfolio.Infra.Services.Email;
using Portfolio.IntegrationTests.Fixtures;

namespace Portfolio.IntegrationTests.Email;

public sealed class EmailQueueTests(PostgreSqlFixtures fixture)
    : IClassFixture<PostgreSqlFixtures>
{
    [Fact]
    public async Task EnqueueAsync_ShouldPersistEmailAndOutboxMessage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        await using var dbContext = fixture.CreateDbContext();

        var notifier = new Mock<IEmailOutboxNotifier>();

        var queue = new EmailQueue(
            NullLogger<EmailQueue>.Instance,
            new EmailRepository(dbContext),
            new EmailOutboxRepository(dbContext),
            dbContext,
            new HttpContextAccessor(),
            notifier.Object);

        // Act
        await queue.EnqueueAsync(message, cancellationToken);

        // Assert
        await using var verificationContext = fixture.CreateDbContext();

        var persistedEmail = await verificationContext.EmailMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == message.Id, cancellationToken);

        var persistedOutbox = await verificationContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.EmailMessageId == message.Id, cancellationToken);

        persistedEmail.Recipient.Should().Be(message.RecipientEmail.Address);
        persistedEmail.Sender.Should().Be(message.SenderEmail.Address);
        persistedEmail.Subject.Should().Be(message.Subject);
        persistedEmail.Status.Should().Be(EmailStatus.Pending);

        persistedOutbox.PayloadJson.Should().NotBeNullOrEmpty();
        persistedOutbox.ProcessedUtc.Should().BeNull();

        notifier.Verify(
            x => x.NotifyAsync(message.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldRollbackEmailWhenOutboxPersistenceFails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        await using var setupContext = fixture.CreateDbContext();

        await setupContext.EmailOutboxes.AddAsync(new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = message.Id,
            PayloadJson = """{"existing":true}""",
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken);

        await setupContext.SaveChangesAsync(cancellationToken);

        await using var dbContext = fixture.CreateDbContext();

        var notifier = new Mock<IEmailOutboxNotifier>();

        var queue = new EmailQueue(
            NullLogger<EmailQueue>.Instance,
            new EmailRepository(dbContext),
            new EmailOutboxRepository(dbContext),
            dbContext,
            new HttpContextAccessor(),
            notifier.Object);

        // Act
        var act = () => queue.EnqueueAsync(message, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();

        await using var verificationContext = fixture.CreateDbContext();

        var emailExists = await verificationContext.EmailMessages
            .AnyAsync(x => x.Id == message.Id, cancellationToken);

        emailExists.Should().BeFalse();

        var outbox = await verificationContext.EmailOutboxes
            .SingleAsync(x => x.EmailMessageId == message.Id, cancellationToken);

        outbox.PayloadJson.Should().Be("""{"existing":true}""");

        notifier.Verify(
            x => x.NotifyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldPreserveRecordsWhenNotificationFails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        await using var dbContext = fixture.CreateDbContext();

        var notifier = new Mock<IEmailOutboxNotifier>();

        notifier
            .Setup(x => x.NotifyAsync(message.Id, cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Notification failed."));

        var queue = new EmailQueue(
            NullLogger<EmailQueue>.Instance,
            new EmailRepository(dbContext),
            new EmailOutboxRepository(dbContext),
            dbContext,
            new HttpContextAccessor(),
            notifier.Object);

        // Act
        var act = () => queue.EnqueueAsync(message, cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();

        await using var verificationContext = fixture.CreateDbContext();

        (await verificationContext.EmailMessages
            .AnyAsync(x => x.Id == message.Id, cancellationToken))
            .Should().BeTrue();

        (await verificationContext.EmailOutboxes
            .AnyAsync(x => x.EmailMessageId == message.Id, cancellationToken))
            .Should().BeTrue();

        notifier.Verify(
            x => x.NotifyAsync(message.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldNotifyAfterTransactionCommits()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
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
                Address = "sender@example.com",
                DisplayName = "Sender"
            },
            Subject = "Integration test",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        await using var dbContext = fixture.CreateDbContext();

        var notifier = new Mock<IEmailOutboxNotifier>();

        notifier
            .Setup(x => x.NotifyAsync(message.Id, cancellationToken))
            .Returns(async () =>
            {
                await using var verificationContext = fixture.CreateDbContext();

                (await verificationContext.EmailMessages
                    .AnyAsync(x => x.Id == message.Id, cancellationToken))
                    .Should().BeTrue();

                (await verificationContext.EmailOutboxes
                    .AnyAsync(x => x.EmailMessageId == message.Id, cancellationToken))
                    .Should().BeTrue();
            });

        var queue = new EmailQueue(
            NullLogger<EmailQueue>.Instance,
            new EmailRepository(dbContext),
            new EmailOutboxRepository(dbContext),
            dbContext,
            new HttpContextAccessor(),
            notifier.Object);

        // Act
        await queue.EnqueueAsync(message, cancellationToken);

        // Assert
        notifier.Verify(
            x => x.NotifyAsync(message.Id, cancellationToken),
            Times.Once);
    }
}