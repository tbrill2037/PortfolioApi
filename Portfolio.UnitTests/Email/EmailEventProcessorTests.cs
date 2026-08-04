using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Data;
using Portfolio.Infra.Services.Email;
using Xunit;

namespace Portfolio.UnitTests.Email;

public class EmailEventProcessorTests
{
    private readonly Mock<IEmailRepository> _emailRepo = new();
    private readonly Mock<IEmailEventRepository> _eventRepo = new();
    private readonly Mock<ILogger<EmailEventProcessor>> _logger = new();
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public EmailEventProcessorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
    }

    private EmailEventProcessor CreateProcessor()
    {
        return new EmailEventProcessor(
            _db,
            _emailRepo.Object,
            _eventRepo.Object,
            _logger.Object);
    }

    private static EmailEvent CreateDeliveryEvent()
    {
        return new EmailEvent
        {
            SesEvent = new SesEvent
            {
                EventType = "Delivery",
                Mail = new SesMail
                {
                    MessageId = "ses-1"
                }
            },
            RawPayload = "{}",
            SnsMessageId = "sns-1",
            TopicArn = "arn:aws:sns:test",
            ReceivedUtc = DateTime.UtcNow
        };
    }

    private void SetupEventRepository(bool alreadyProcessed = false)
    {
        _eventRepo
            .Setup(x => x.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<EmailEventType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyProcessed);

        _eventRepo
            .Setup(x => x.AddEventRecordAsync(
                It.IsAny<EmailMessageEntity>(),
                It.IsAny<EmailEvent>(),
                It.IsAny<EmailEventType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIgnoreAlreadyProcessedEvent()
    {
        SetupEventRepository(alreadyProcessed: true);

        var processor = CreateProcessor();

        await processor.ProcessAsync(CreateDeliveryEvent(), CancellationToken.None);

        _emailRepo.Verify(
            x => x.GetBySesMessageIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _eventRepo.Verify(
            x => x.AddEventRecordAsync(
                It.IsAny<EmailMessageEntity>(),
                It.IsAny<EmailEvent>(),
                It.IsAny<EmailEventType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMoveSentEmailToDelivered()
    {
        var email = new EmailMessageEntity
        {
            Id = Guid.NewGuid(),
            SesMessageId = "ses-1",
            Status = EmailStatus.Sent,
            Recipient = "john.doe@example.com",
            RecipientName = "John Doe",
            Sender = "tony@tonybrill.dev",
            SenderName = "Tony Brill",
            Subject = "New Email",
            TemplatePath = EmailType.Contact.ToString(),
        };

        _emailRepo
            .Setup(x => x.GetBySesMessageIdAsync(
                "ses-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(email);

        SetupEventRepository();

        var processor = CreateProcessor();

        await processor.ProcessAsync(CreateDeliveryEvent(), CancellationToken.None);

        email.Status.Should().Be(EmailStatus.Delivered);

        _eventRepo.Verify(
            x => x.AddEventRecordAsync(
                email,
                It.IsAny<EmailEvent>(),
                EmailEventType.Delivery,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldIgnoreMissingEmail()
    {
        _emailRepo
            .Setup(x => x.GetBySesMessageIdAsync(
                "ses-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailMessageEntity?)null);

        SetupEventRepository();

        var processor = CreateProcessor();

        await processor.ProcessAsync(CreateDeliveryEvent(), CancellationToken.None);

        _eventRepo.Verify(
            x => x.AddEventRecordAsync(
                It.IsAny<EmailMessageEntity>(),
                It.IsAny<EmailEvent>(),
                It.IsAny<EmailEventType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}