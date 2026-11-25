using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;
using Portfolio.Infra.Repository.Email;
using Portfolio.Infra.Services.Email;

namespace Portfolio.IntegrationTests.Email;

public sealed class EmailEventProcessorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public EmailEventProcessorTests()
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
    public async Task ProcessAsync_ShouldPersistEventAndUpdateEmailStatus()
    {
        // Arrange
        const string providerMessageId = "provider-message-1";
        var cancellationToken = TestContext.Current.CancellationToken;

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "john.doe@example.com",
            RecipientName = "John Doe",
            Sender = "test@example.com",
            SenderName = "Jane Doe",
            Subject = "Test Email",
            TemplatePath = "ContactMe",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent,
            ProviderMessageId = providerMessageId,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.EmailMessages.AddAsync(email, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Use a fresh context so the processor must retrieve the email
        // from the database rather than reuse the seeded tracked entity.
        await _dbContext.DisposeAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var emailRepository = new EmailRepository(dbContext);
        var eventRepository = new EmailEventRepository(dbContext);

        var processor = new EmailEventProcessor(
            dbContext,
            emailRepository,
            eventRepository,
            NullLogger<EmailEventProcessor>.Instance);

        var emailEvent = new Portfolio.Application.Common.Models.Email.EmailEvent
        {
            ProviderMessageId = providerMessageId,
            EventType = EmailEventType.Delivery,
            RawPayload = """{"event":"delivery"}""",
            ReceivedUtc = DateTime.UtcNow
        };

        // Act
        await processor.ProcessAsync(emailEvent, cancellationToken);

        // Assert
        var persistedEmail = await dbContext.EmailMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == email.Id, cancellationToken);

        var persistedEvent = await dbContext.EmailEvents
            .AsNoTracking()
            .SingleAsync(x =>
                x.ProviderMessageId == providerMessageId &&
                x.EventType == EmailEventType.Delivery, cancellationToken);

        persistedEmail.Status.Should().Be(EmailStatus.Delivered);

        persistedEvent.EmailMessageId.Should().Be(email.Id);
        persistedEvent.ProviderMessageId.Should().Be(providerMessageId);
        persistedEvent.EventType.Should().Be(EmailEventType.Delivery);
        persistedEvent.PayloadJson.Should().Be("""{"event":"delivery"}""");

        await dbContext.DisposeAsync();
    }

    [Fact]
    public async Task ProcessAsync_ShouldIgnoreAlreadyProcessedEvent()
    {
        // Arrange
        const string providerMessageId = "provider-message-duplicate";
        var cancellationToken = TestContext.Current.CancellationToken;

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "john.doe@example.com",
            RecipientName = "John Doe",
            Sender = "test@example.com",
            SenderName = "Jane Doe",
            Subject = "Test Email",
            TemplatePath = "ContactMe",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent,
            ProviderMessageId = providerMessageId,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.EmailMessages.AddAsync(email, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailRepository = new EmailRepository(_dbContext);
        var eventRepository = new EmailEventRepository(_dbContext);

        var processor = new EmailEventProcessor(
            _dbContext,
            emailRepository,
            eventRepository,
            NullLogger<EmailEventProcessor>.Instance);

        var emailEvent = new Portfolio.Application.Common.Models.Email.EmailEvent
        {
            ProviderMessageId = providerMessageId,
            EventType = EmailEventType.Delivery,
            RawPayload = """{"event":"delivery"}""",
            ReceivedUtc = DateTime.UtcNow
        };

        // Act
        await processor.ProcessAsync(emailEvent, cancellationToken);
        await processor.ProcessAsync(emailEvent, cancellationToken);

        // Assert
        var eventCount = await _dbContext.EmailEvents.CountAsync(
            x => x.ProviderMessageId == providerMessageId &&
                 x.EventType == EmailEventType.Delivery,
            cancellationToken);

        eventCount.Should().Be(1);
    }


    [Fact]
    public async Task ProcessAsync_ShouldIgnoreEventWhenEmailDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var emailRepository = new EmailRepository(_dbContext);
        var eventRepository = new EmailEventRepository(_dbContext);

        var processor = new EmailEventProcessor(
            _dbContext,
            emailRepository,
            eventRepository,
            NullLogger<EmailEventProcessor>.Instance);

        var emailEvent = new Portfolio.Application.Common.Models.Email.EmailEvent
        {
            ProviderMessageId = "missing-email-message",
            EventType = EmailEventType.Delivery,
            RawPayload = """{"event":"delivery"}""",
            ReceivedUtc = DateTime.UtcNow
        };

        // Act
        await processor.ProcessAsync(emailEvent, cancellationToken);

        // Assert
        var eventCount = await _dbContext.EmailEvents.CountAsync(
            x => x.ProviderMessageId == emailEvent.ProviderMessageId,
            cancellationToken);

        eventCount.Should().Be(0);
    }


    [Fact]
    public async Task ProcessAsync_ShouldRecordEventWithoutChangingStatusWhenTransitionIsInvalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string providerMessageId = "provider-message-invalid-transition";

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "john.doe@example.com",
            RecipientName = "John Doe",
            Sender = "test@example.com",
            SenderName = "Jane Doe",
            Subject = "Test Email",
            TemplatePath = "ContactMe",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent,
            ProviderMessageId = providerMessageId,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.EmailMessages.AddAsync(email, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailRepository = new EmailRepository(_dbContext);
        var eventRepository = new EmailEventRepository(_dbContext);

        var processor = new EmailEventProcessor(
            _dbContext,
            emailRepository,
            eventRepository,
            NullLogger<EmailEventProcessor>.Instance);

        var emailEvent = new Portfolio.Application.Common.Models.Email.EmailEvent
        {
            ProviderMessageId = providerMessageId,
            EventType = EmailEventType.Open,
            RawPayload = """{"event":"open"}""",
            ReceivedUtc = DateTime.UtcNow
        };

        // Act
        await processor.ProcessAsync(emailEvent, cancellationToken);

        // Assert
        var persistedEmail = await _dbContext.EmailMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == email.Id, cancellationToken);

        var persistedEvent = await _dbContext.EmailEvents
            .AsNoTracking()
            .SingleAsync(
                x => x.ProviderMessageId == providerMessageId,
                cancellationToken);

        persistedEmail.Status.Should().Be(EmailStatus.Sent);
        persistedEvent.EventType.Should().Be(EmailEventType.Open);
    }


    [Fact]
    public async Task ProcessAsync_ShouldUpdateSentEmailToBounced()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string providerMessageId = "provider-message-bounce";

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "john.doe@example.com",
            RecipientName = "John Doe",
            Sender = "test@example.com",
            SenderName = "Jane Doe",
            Subject = "Test Email",
            TemplatePath = "ContactMe",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent,
            ProviderMessageId = providerMessageId,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.EmailMessages.AddAsync(email, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailRepository = new EmailRepository(_dbContext);
        var eventRepository = new EmailEventRepository(_dbContext);

        var processor = new EmailEventProcessor(
            _dbContext,
            emailRepository,
            eventRepository,
            NullLogger<EmailEventProcessor>.Instance);

        var emailEvent = new Portfolio.Application.Common.Models.Email.EmailEvent
        {
            ProviderMessageId = providerMessageId,
            EventType = EmailEventType.Bounce,
            RawPayload = """{"event":"bounce"}""",
            ReceivedUtc = DateTime.UtcNow
        };

        // Act
        await processor.ProcessAsync(emailEvent, cancellationToken);

        // Assert
        var persistedEmail = await _dbContext.EmailMessages
            .AsNoTracking()
            .SingleAsync(x => x.Id == email.Id, cancellationToken);

        persistedEmail.Status.Should().Be(EmailStatus.Bounced);

        var persistedEvent = await _dbContext.EmailEvents
            .AsNoTracking()
            .SingleAsync(
                x => x.ProviderMessageId == providerMessageId,
                cancellationToken);

        persistedEvent.EventType.Should().Be(EmailEventType.Bounce);
    }


    [Fact]
    public async Task EmailEventRepository_ShouldRejectDuplicateProviderEvents()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string providerMessageId = "provider-message-unique";

        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Recipient = "john.doe@example.com",
            RecipientName = "John Doe",
            Sender = "test@example.com",
            SenderName = "Jane Doe",
            Subject = "Test Email",
            TemplatePath = "ContactMe",
            TemplateType = EmailType.Contact,
            Status = EmailStatus.Sent,
            ProviderMessageId = providerMessageId,
            CreatedUtc = DateTime.UtcNow
        };

        await _dbContext.EmailMessages.AddAsync(email, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var firstEvent = new EmailEventEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            ProviderMessageId = providerMessageId,
            EventType = EmailEventType.Delivery,
            PayloadJson = """{"event":"delivery"}""",
            CreatedUtc = DateTime.UtcNow,
            ReceivedUtc = DateTime.UtcNow
        };

        var duplicateEvent = new EmailEventEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email.Id,
            ProviderMessageId = providerMessageId,
            EventType = EmailEventType.Delivery,
            PayloadJson = """{"event":"delivery"}""",
            CreatedUtc = DateTime.UtcNow,
            ReceivedUtc = DateTime.UtcNow
        };

        // Act
        await _dbContext.EmailEvents.AddAsync(firstEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.EmailEvents.AddAsync(duplicateEvent, cancellationToken);

        // Assert
        var exception = await FluentActions
            .Invoking(() => _dbContext.SaveChangesAsync(cancellationToken))
            .Should()
            .ThrowAsync<DbUpdateException>();

        exception.Which.Should().NotBeNull();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
