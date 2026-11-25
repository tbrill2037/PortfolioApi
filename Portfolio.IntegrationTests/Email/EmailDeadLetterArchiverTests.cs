using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;
using Portfolio.Infra.Repository.Email;
using Portfolio.Infra.Services.Email;

namespace Portfolio.IntegrationTests.Email;

public sealed class EmailDeadLetterArchiverTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public EmailDeadLetterArchiverTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task ArchiveAsync_ShouldPersistDeadLetterAndMarkEmailAsFailed()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "test@example.com",
            RecipientName = "Test User",
            Sender = "noreply@example.com",
            SenderName = "Portfolio",
            Subject = "Test email",
            TemplatePath = "emails/test",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent
        };

        _dbContext.EmailMessages.Add(email);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var deadLetterRepository = new EmailDeadLetterRepository(_dbContext);
        var emailRepository = new EmailRepository(_dbContext);

        var archiver = new EmailDeadLetterArchiver(
            deadLetterRepository,
            emailRepository,
            _dbContext,
            NullLogger<EmailDeadLetterArchiver>.Instance);

        var message = new EmailDeadLetterMessage(
            QueueMessageId: "queue-message-123",
            Body: "{\"email\":\"test\"}",
            ReceiveCount: 3);

        var emailMessage = new EmailMessage
        {
            Id = email.Id,
            CorrelationId = email.CorrelationId,
            RecipientEmail = new EmailAddress
            {
                Address = "test@example.com",
                DisplayName = "Test User"
            },
            SenderEmail = new EmailAddress
            {
                Address = "noreply@example.com",
                DisplayName = "Portfolio"
            },
            Subject = email.Subject,
            TemplateType = email.TemplateType,
            TemplatePath = email.TemplatePath,
            Resources = []
        };

        var exception = new Exception("Email sending failure");

        // Act
        await archiver.ArchiveAsync(
            message,
            emailMessage,
            exception,
            cancellationToken);

        // Assert
        var deadLetter = await _dbContext.EmailDeadLetters
            .SingleAsync(x => x.QueueMessageId == message.QueueMessageId, cancellationToken);

        deadLetter.EmailMessageId.Should().Be(email.Id);
        deadLetter.Payload.Should().Be(message.Body);
        deadLetter.FailureReason.Should().Be(exception.Message);
        deadLetter.ExceptionType.Should().Be(exception.GetType().FullName);
        deadLetter.ReceiveCount.Should().Be(3);

        var updatedEmail = await _dbContext.EmailMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == email.Id, cancellationToken);

        updatedEmail.Status.Should().Be(EmailStatus.Failed);
        updatedEmail.ErrorMessage.Should().Be(exception.Message);
        updatedEmail.DeadLetteredUtc.Should().NotBeNull();
        updatedEmail.LastErrorUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ArchiveAsync_WithoutEmail_ShouldPersistDeadLetterWithoutEmailReference()
    {
        // Arrange
        var deadLetterRepository = new EmailDeadLetterRepository(_dbContext);
        var emailRepository = new EmailRepository(_dbContext);

        var cancellationToken = TestContext.Current.CancellationToken;

        var archiver = new EmailDeadLetterArchiver(
            deadLetterRepository,
            emailRepository,
            _dbContext,
            NullLogger<EmailDeadLetterArchiver>.Instance);

        var message = new EmailDeadLetterMessage(
            QueueMessageId: "queue-message-456",
            Body: "{\"email\":\"unknown\"}",
            ReceiveCount: 3);

        // Act
        await archiver.ArchiveAsync(
            message,
            null,
            null,
            cancellationToken);

        // Assert
        var deadLetter = await _dbContext.EmailDeadLetters
            .SingleAsync(x => x.QueueMessageId == message.QueueMessageId, cancellationToken);

        deadLetter.EmailMessageId.Should().BeNull();
        deadLetter.Payload.Should().Be(message.Body);
        deadLetter.FailureReason.Should().Be(
            "Message moved to dead letter queue after retries were exhausted.");
        deadLetter.ExceptionType.Should().BeNull();
        deadLetter.ReceiveCount.Should().Be(3);
    }

    [Fact]
    public async Task ArchiveAsync_WhenDeadLetterInsertFails_ShouldRollbackEmailUpdate()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "test@example.com",
            RecipientName = "Test User",
            Sender = "noreply@example.com",
            SenderName = "Portfolio",
            Subject = "Test email",
            TemplatePath = "emails/test",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent
        };

        _dbContext.EmailMessages.Add(email);

        _dbContext.EmailDeadLetters.Add(new EmailDeadLetterEntity
        {
            Id = Guid.NewGuid(),
            QueueMessageId = "duplicate-queue-id",
            Payload = "{}",
            FailureReason = "Existing failure",
            ReceiveCount = 3,
            CreatedUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailMessage = new EmailMessage
        {
            Id = email.Id,
            CorrelationId = email.CorrelationId,
            RecipientEmail = new EmailAddress
            {
                Address = "test@example.com",
                DisplayName = "Test User"
            },
            SenderEmail = new EmailAddress
            {
                Address = "noreply@example.com",
                DisplayName = "Portfolio"
            },
            Subject = email.Subject,
            TemplateType = email.TemplateType,
            TemplatePath = email.TemplatePath,
            Resources = []
        };

        var archiver = new EmailDeadLetterArchiver(
            new EmailDeadLetterRepository(_dbContext),
            new EmailRepository(_dbContext),
            _dbContext,
            NullLogger<EmailDeadLetterArchiver>.Instance);

        var message = new EmailDeadLetterMessage(
            QueueMessageId: "duplicate-queue-id",
            Body: "{\"email\":\"test\"}",
            ReceiveCount: 3);

        // Act
        var act = () => archiver.ArchiveAsync(
            message,
            emailMessage,
            new Exception("Email sending failure"),
            cancellationToken);

        // Assert
        await FluentActions.Invoking(act)
            .Should()
            .ThrowAsync<DbUpdateException>();

        var updatedEmail = await _dbContext.EmailMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == email.Id, cancellationToken);

        updatedEmail.Status.Should().Be(EmailStatus.Sent);
        updatedEmail.DeadLetteredUtc.Should().BeNull();

        var deadLetterCount = await _dbContext.EmailDeadLetters
            .CountAsync(x => x.QueueMessageId == "duplicate-queue-id", cancellationToken);

        deadLetterCount.Should().Be(1);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}