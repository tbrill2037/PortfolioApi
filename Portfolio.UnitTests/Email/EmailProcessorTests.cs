using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portfolio.Application.Common.Exceptions.Email;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Services.Email;
using Xunit;

namespace Portfolio.UnitTests.Email;

public sealed class EmailProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ShouldReturnLockedWhenEmailIsAlreadyProcessing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var existingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                false,
                null,
                existingLockUntilUtc,
                ProcessingLockFailureReason.AlreadyLocked));

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.Locked);
        result.CorrelationId.Should().Be(correlationId);

        composer.Verify(
            x => x.ComposeAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnAlreadyProcessedWhenEmailIsCompleted()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                false,
                null,
                null,
                ProcessingLockFailureReason.AlreadyCompleted));

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.AlreadyProcessed);
        result.CorrelationId.Should().Be(correlationId);

        composer.Verify(
            x => x.ComposeAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnNotFoundWhenEmailDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                false,
                null,
                null,
                ProcessingLockFailureReason.NotFound));

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.NotFound);
        result.CorrelationId.Should().Be(correlationId);

        composer.Verify(
            x => x.ComposeAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldComposeSendAndMarkEmailAsSent()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();
        const string providerMessageId = "provider-message-123";

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ReturnsAsync(new EmailContent
            {
                HtmlBody = "<h1>Hello</h1>",
                TextBody = "Hello"
            });

        sender
            .Setup(x => x.SendAsync(
                email,
                "<h1>Hello</h1>",
                "Hello",
                cancellationToken))
            .ReturnsAsync(providerMessageId);

        emailRepository
            .Setup(x => x.MarkSentAsync(
                emailId,
                lockToken,
                providerMessageId,
                cancellationToken))
            .ReturnsAsync(true);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.Sent);
        result.CorrelationId.Should().Be(correlationId);
        result.ProviderMessageId.Should().Be(providerMessageId);

        composer.Verify(
            x => x.ComposeAsync(email, cancellationToken),
            Times.Once);

        quotaService.Verify(
            x => x.CheckAsync(cancellationToken),
            Times.Once);

        rateLimiter.Verify(
            x => x.WaitAsync(cancellationToken),
            Times.Once);

        providerRateLimiter.Verify(
            x => x.WaitAsync(cancellationToken),
            Times.Once);

        sender.Verify(
            x => x.SendAsync(
                email,
                "<h1>Hello</h1>",
                "Hello",
                cancellationToken),
            Times.Once);

        emailRepository.Verify(
            x => x.MarkSentAsync(
                emailId,
                lockToken,
                providerMessageId,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMarkEmailFailedWhenTemplateIsNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();
        const string templatePath = "emails/contact.scriban";

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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
            TemplatePath = templatePath,
            Resources = []
        };

        var exception = new EmailTemplateNotFoundException(
            templatePath,
            new FileNotFoundException());

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ThrowsAsync(exception);

        emailRepository
            .Setup(x => x.MarkFailedAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken))
            .ReturnsAsync(true);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.PermanentFailure);
        result.CorrelationId.Should().Be(correlationId);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken),
            Times.Once);

        quotaService.Verify(
            x => x.CheckAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        rateLimiter.Verify(
            x => x.WaitAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        providerRateLimiter.Verify(
            x => x.WaitAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMarkEmailFailedWhenTemplateModelIsInvalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();
        const string templatePath = "emails/contact.scriban";

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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
            TemplatePath = templatePath,
            Resources = []
        };

        var exception = new InvalidTemplateModelException(templatePath);

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ThrowsAsync(exception);

        emailRepository
            .Setup(x => x.MarkFailedAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken))
            .ReturnsAsync(true);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.PermanentFailure);
        result.CorrelationId.Should().Be(correlationId);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken),
            Times.Once);

        quotaService.Verify(
            x => x.CheckAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        rateLimiter.Verify(
            x => x.WaitAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        providerRateLimiter.Verify(
            x => x.WaitAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRecordRetryableFailureWhenSendingThrowsHttpRequestException()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        var content = new EmailContent
        {
            HtmlBody = "<h1>Hello</h1>",
            TextBody = "Hello"
        };

        var exception = new HttpRequestException("Provider unavailable.");

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ReturnsAsync(content);

        sender
            .Setup(x => x.SendAsync(
                email,
                content.HtmlBody,
                content.TextBody,
                cancellationToken))
            .ThrowsAsync(exception);

        emailRepository
            .Setup(x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken))
            .ReturnsAsync(true);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.RetryableFailure);
        result.CorrelationId.Should().Be(correlationId);

        emailRepository.Verify(
            x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken),
            Times.Once);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        emailRepository.Verify(
            x => x.MarkSentAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRecordFailureAndRethrowUnexpectedException()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        var content = new EmailContent
        {
            HtmlBody = "<h1>Hello</h1>",
            TextBody = "Hello"
        };

        var exception = new InvalidOperationException("Unexpected processing failure.");

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ReturnsAsync(content);

        sender
            .Setup(x => x.SendAsync(
                email,
                content.HtmlBody,
                content.TextBody,
                cancellationToken))
            .ThrowsAsync(exception);

        emailRepository
            .Setup(x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken))
            .ReturnsAsync(true);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var act = () => processor.ProcessAsync(email, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(exception.Message);

        emailRepository.Verify(
            x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken),
            Times.Once);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRecordFailureAndThrowWhenProcessingLockIsLostWhileMarkingSent()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();
        const string providerMessageId = "provider-message-123";

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        var content = new EmailContent
        {
            HtmlBody = "<h1>Hello</h1>",
            TextBody = "Hello"
        };

        const string expectedError = "Lost processing lock while marking email sent.";

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ReturnsAsync(content);

        sender
            .Setup(x => x.SendAsync(
                email,
                content.HtmlBody,
                content.TextBody,
                cancellationToken))
            .ReturnsAsync(providerMessageId);

        emailRepository
            .Setup(x => x.MarkSentAsync(
                emailId,
                lockToken,
                providerMessageId,
                cancellationToken))
            .ReturnsAsync(false);

        emailRepository
            .Setup(x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                expectedError,
                cancellationToken))
            .ReturnsAsync(true);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var act = () => processor.ProcessAsync(email, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(expectedError);

        emailRepository.Verify(
            x => x.MarkSentAsync(
                emailId,
                lockToken,
                providerMessageId,
                cancellationToken),
            Times.Once);

        emailRepository.Verify(
            x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                expectedError,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnPermanentFailureWhenMarkFailedReturnsFalse()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();
        const string templatePath = "emails/contact.scriban";

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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
            TemplatePath = templatePath,
            Resources = []
        };

        var exception = new EmailTemplateNotFoundException(
            templatePath,
            new FileNotFoundException());

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ThrowsAsync(exception);

        emailRepository
            .Setup(x => x.MarkFailedAsync(
                emailId,
                lockToken,
                It.IsAny<string>(),
                cancellationToken))
            .ReturnsAsync(false);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.PermanentFailure);
        result.CorrelationId.Should().Be(correlationId);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken),
            Times.Once);

        quotaService.Verify(
            x => x.CheckAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        rateLimiter.Verify(
            x => x.WaitAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        providerRateLimiter.Verify(
            x => x.WaitAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnRetryableFailureWhenFailedAttemptCannotBeRecorded()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        var content = new EmailContent
        {
            HtmlBody = "<h1>Hello</h1>",
            TextBody = "Hello"
        };

        var exception = new HttpRequestException("Provider unavailable.");

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ReturnsAsync(content);

        sender
            .Setup(x => x.SendAsync(
                email,
                content.HtmlBody,
                content.TextBody,
                cancellationToken))
            .ThrowsAsync(exception);

        emailRepository
            .Setup(x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                It.IsAny<string>(),
                cancellationToken))
            .ReturnsAsync(false);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var result = await processor.ProcessAsync(email, cancellationToken);

        // Assert
        result.Result.Should().Be(EmailProcessResult.RetryableFailure);
        result.CorrelationId.Should().Be(correlationId);

        emailRepository.Verify(
            x => x.RecordFailedAttemptAsync(
                emailId,
                lockToken,
                exception.Message,
                cancellationToken),
            Times.Once);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        emailRepository.Verify(
            x => x.MarkSentAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldThrowWhenProcessingLockIsAcquiredWithoutToken()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                null,
                null,
                ProcessingLockFailureReason.None));

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var act = () => processor.ProcessAsync(email, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Processing lock acquired without token.");

        composer.Verify(
            x => x.ComposeAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        emailRepository.Verify(
            x => x.RecordFailedAttemptAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReleaseProcessingLockWhenCanceled()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var emailId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var lockToken = Guid.NewGuid();

        var emailRepository = new Mock<IEmailRepository>();
        var composer = new Mock<IEmailComposer>();
        var sender = new Mock<IEmailSender>();
        var quotaService = new Mock<IEmailQuotaService>();
        var rateLimiter = new Mock<IEmailRateLimiter>();
        var providerRateLimiter = new Mock<IEmailProviderRateLimiter>();

        var email = new EmailMessage
        {
            Id = emailId,
            CorrelationId = correlationId,
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

        var cancellationException = new OperationCanceledException("Processing canceled.");

        emailRepository
            .Setup(x => x.TryAcquireProcessingLockAsync(
                emailId,
                It.IsAny<DateTime>(),
                cancellationToken))
            .ReturnsAsync(new ProcessingLockResult(
                true,
                lockToken,
                null,
                ProcessingLockFailureReason.None));

        composer
            .Setup(x => x.ComposeAsync(email, cancellationToken))
            .ThrowsAsync(cancellationException);

        var processor = new EmailProcessor(
            emailRepository.Object,
            composer.Object,
            sender.Object,
            quotaService.Object,
            rateLimiter.Object,
            providerRateLimiter.Object,
            NullLogger<EmailProcessor>.Instance);

        // Act
        var act = () => processor.ProcessAsync(email, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<OperationCanceledException>()
            .WithMessage("Processing canceled.");

        emailRepository.Verify(
            x => x.ReleaseProcessingLockAsync(
                emailId,
                lockToken,
                CancellationToken.None),
            Times.Once);

        emailRepository.Verify(
            x => x.RecordFailedAttemptAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        emailRepository.Verify(
            x => x.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        sender.Verify(
            x => x.SendAsync(
                It.IsAny<EmailMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}