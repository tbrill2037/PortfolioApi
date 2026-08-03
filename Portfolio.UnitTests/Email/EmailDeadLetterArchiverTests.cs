using Amazon.Lambda.SQSEvents;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Infra.Data;
using Portfolio.Infra.Services.Email;
using Xunit;

namespace Portfolio.UnitTests.Email;

public class EmailDeadLetterArchiverTests
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public EmailDeadLetterArchiverTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task ArchiveAsync_ShouldCreateDeadLetterRecord()
    {
        // Arrange
        var deadLetterRepository = new Mock<IEmailDeadLetterRepository>();

        EmailDeadLetterEntity? savedEntity = null;

        deadLetterRepository
            .Setup(x => x.AddAsync(
                It.IsAny<EmailDeadLetterEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailDeadLetterEntity, CancellationToken>(
                (entity, _) => savedEntity = entity)
            .Returns(Task.CompletedTask);

        var emailRepository = new Mock<IEmailRepository>();

        var logger = Mock.Of<ILogger<EmailDeadLetterArchiver>>();

        var archiver = new EmailDeadLetterArchiver(
            deadLetterRepository.Object,
            emailRepository.Object,
            _dbContext,
            logger);

        var message = new SQSEvent.SQSMessage
        {
            MessageId = "123",
            Body = "{\"email\":\"test\"}",
            Attributes = new Dictionary<string, string>
            {
                ["ApproximateReceiveCount"] = "3"
            }
        };

        // Act
        await archiver.ArchiveAsync(
            message,
            null,
            new Exception("SES failure"),
            CancellationToken.None);

        // Assert
        savedEntity.Should().NotBeNull();
        savedEntity!.SqsMessageId.Should().Be("123");
        savedEntity.FailureReason.Should().Be("SES failure");
        savedEntity.ExceptionType.Should().Contain("Exception");
        savedEntity.ReceiveCount.Should().Be(3);

        deadLetterRepository.Verify(
            x => x.AddAsync(
                It.IsAny<EmailDeadLetterEntity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}