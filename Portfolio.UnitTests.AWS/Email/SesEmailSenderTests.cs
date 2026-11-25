using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Services.Email;
using Portfolio.Infra.Email.Configuration;

namespace Portfolio.UnitTests.AWS.Email;

public sealed class SesEmailSenderTests
{
    [Fact]
    public async Task SendAsync_ShouldSendRawMimeEmailThroughSes()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string providerMessageId = "ses-message-123";
        const string configurationSet = "portfolio-email-events";
        const string fromEmail = "noreply@example.com";
        const string fromName = "Portfolio Studio";

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = configurationSet,
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = fromName,
            FromEmail = fromEmail
        });

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
                Address = fromEmail,
                DisplayName = fromName
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        SendEmailRequest? capturedRequest = null;
        var rawStreamStartsAtBeginning = false;

        ses.Setup(x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken))
            .Callback<SendEmailRequest, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
                rawStreamStartsAtBeginning = request.Content.Raw.Data.Position == 0;
            })
            .ReturnsAsync(new SendEmailResponse
            {
                MessageId = providerMessageId
            });

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var result = await sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        result.Should().Be(providerMessageId);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.FromEmailAddress.Should().Be(fromEmail);
        capturedRequest.ConfigurationSetName.Should().Be(configurationSet);
        capturedRequest.Content.Raw.Should().NotBeNull();

        rawStreamStartsAtBeginning.Should().BeTrue();

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken),
            Times.Once);

        resourceResolver.Verify(
            x => x.ResolveAsync(
                It.IsAny<EmailResource>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowWhenMessageIsNull()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var act = () => sender.SendAsync(
            null!,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithParameterName("message");

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        resourceResolver.Verify(
            x => x.ResolveAsync(
                It.IsAny<EmailResource>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeResolvedAttachmentInRawMimeMessage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string providerMessageId = "ses-message-123";
        const string fileName = "report.pdf";
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        var resource = new EmailResource
        {
            FileName = fileName,
            ContentType = "application/pdf",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = [resource]
        };

        resourceResolver
            .Setup(x => x.ResolveAsync(resource, cancellationToken))
            .ReturnsAsync(fileContent);

        byte[]? capturedMimeContent = null;

        ses.Setup(x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken))
            .Callback<SendEmailRequest, CancellationToken>((request, _) =>
            {
                using var stream = new MemoryStream();
                request.Content.Raw.Data.CopyTo(stream);
                capturedMimeContent = stream.ToArray();
            })
            .ReturnsAsync(new SendEmailResponse
            {
                MessageId = providerMessageId
            });

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var result = await sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        result.Should().Be(providerMessageId);
        capturedMimeContent.Should().NotBeNull();

        using var stream = new MemoryStream(capturedMimeContent!);

        var mimeMessage = await MimeKit.MimeMessage.LoadAsync(
            stream,
            cancellationToken);

        mimeMessage.Attachments.Should().ContainSingle();

        var attachment = mimeMessage.Attachments.Single();

        attachment.ContentDisposition!.Disposition
            .Should().Be(MimeKit.ContentDisposition.Attachment);

        attachment.ContentDisposition.FileName.Should().Be(fileName);

        resourceResolver.Verify(
            x => x.ResolveAsync(resource, cancellationToken),
            Times.Once);

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeResolvedInlineResourceInRawMimeMessage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string providerMessageId = "ses-message-123";
        const string fileName = "logo.png";
        const string contentId = "portfolio-logo";
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        var resource = new EmailResource
        {
            FileName = fileName,
            ContentType = "image/png",
            ResourceKey = "images/logo.png",
            Type = EmailResourceType.External,
            ContentId = contentId,
            IsInline = true
        };

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = [resource]
        };

        resourceResolver
            .Setup(x => x.ResolveAsync(resource, cancellationToken))
            .ReturnsAsync(fileContent);

        byte[]? capturedMimeContent = null;

        ses.Setup(x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken))
            .Callback<SendEmailRequest, CancellationToken>((request, _) =>
            {
                using var stream = new MemoryStream();
                request.Content.Raw.Data.CopyTo(stream);
                capturedMimeContent = stream.ToArray();
            })
            .ReturnsAsync(new SendEmailResponse
            {
                MessageId = providerMessageId
            });

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var result = await sender.SendAsync(
            email,
            "<h1>Hello</h1><img src=\"cid:portfolio-logo\">",
            "Hello",
            cancellationToken);

        // Assert
        result.Should().Be(providerMessageId);
        capturedMimeContent.Should().NotBeNull();

        using var stream = new MemoryStream(capturedMimeContent!);

        var mimeMessage = await MimeKit.MimeMessage.LoadAsync(
            stream,
            cancellationToken);

        var inlineResource = mimeMessage.BodyParts
            .OfType<MimeKit.MimePart>()
            .Single(x => x.ContentId == contentId);

        inlineResource.ContentDisposition!.Disposition
            .Should().Be(MimeKit.ContentDisposition.Inline);

        inlineResource.ContentDisposition.FileName.Should().Be(fileName);

        mimeMessage.Attachments.Should().BeEmpty();

        resourceResolver.Verify(
            x => x.ResolveAsync(resource, cancellationToken),
            Times.Once);

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowWhenResolvedResourceIsEmpty()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = [resource]
        };

        resourceResolver
            .Setup(x => x.ResolveAsync(resource, cancellationToken))
            .ReturnsAsync([]);

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var act = () => sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Email resource 'report.pdf' resolved to an empty file.");

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        resourceResolver.Verify(
            x => x.ResolveAsync(resource, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowWhenResourceContentTypeIsInvalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = [resource]
        };

        resourceResolver
            .Setup(x => x.ResolveAsync(resource, cancellationToken))
            .ReturnsAsync([1, 2, 3]);

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var act = () => sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid email resource content type 'application'.");

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        resourceResolver.Verify(
            x => x.ResolveAsync(resource, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowWhenInlineResourceHasNoContentId()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        var resource = new EmailResource
        {
            FileName = "logo.png",
            ContentType = "image/png",
            ResourceKey = "images/logo.png",
            Type = EmailResourceType.External,
            IsInline = true
        };

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = [resource]
        };

        resourceResolver
            .Setup(x => x.ResolveAsync(resource, cancellationToken))
            .ReturnsAsync([1, 2, 3]);

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var act = () => sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Inline email resource 'logo.png' requires a ContentId.");

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        resourceResolver.Verify(
            x => x.ResolveAsync(resource, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldRethrowWhenSesRequestFails()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var exception = new AmazonSimpleEmailServiceV2Exception("SES request failed.")
        {
            ErrorCode = "MessageRejected"
        };

        ses.Setup(x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken))
            .ThrowsAsync(exception);

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var act = () => sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        var thrownException = await act.Should()
            .ThrowAsync<AmazonSimpleEmailServiceV2Exception>();

        thrownException.Which.Should().BeSameAs(exception);
        thrownException.Which.ErrorCode.Should().Be("MessageRejected");

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldRethrowWhenSesRequestIsCancelled()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Test email",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        var exception = new OperationCanceledException("SES request cancelled.");

        ses.Setup(x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken))
            .ThrowsAsync(exception);

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var act = () => sender.SendAsync(
            email,
            "<h1>Hello</h1>",
            "Hello",
            cancellationToken);

        // Assert
        var thrownException = await act.Should()
            .ThrowAsync<OperationCanceledException>();

        thrownException.Which.Should().BeSameAs(exception);

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken),
            Times.Once);

        resourceResolver.Verify(
            x => x.ResolveAsync(
                It.IsAny<EmailResource>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeExpectedHeadersAndBodyInRawMimeMessage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var ses = new Mock<IAmazonSimpleEmailServiceV2>();
        var resourceResolver = new Mock<IEmailResourceResolver>();

        var sesOptions = Options.Create(new SesOptions
        {
            ConfigurationSet = "portfolio-email-events",
            EventTopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents"
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromName = "Portfolio Studio",
            FromEmail = "noreply@example.com"
        });

        const string htmlBody = "<h1>Welcome</h1><p>Thanks for contacting us.</p>";
        const string textBody = "Welcome. Thanks for contacting us.";

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
                Address = "noreply@example.com",
                DisplayName = "Portfolio Studio"
            },
            Subject = "Welcome to Portfolio Studio",
            TemplateType = EmailType.Contact,
            TemplatePath = "emails/contact.scriban",
            Resources = []
        };

        byte[]? capturedMimeContent = null;

        ses.Setup(x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken))
            .Callback<SendEmailRequest, CancellationToken>((request, _) =>
            {
                using var stream = new MemoryStream();
                request.Content.Raw.Data.CopyTo(stream);
                capturedMimeContent = stream.ToArray();
            })
            .ReturnsAsync(new SendEmailResponse
            {
                MessageId = "ses-message-123"
            });

        var sender = new SesEmailSender(
            ses.Object,
            sesOptions,
            emailOptions,
            NullLogger<SesEmailSender>.Instance,
            resourceResolver.Object);

        // Act
        var result = await sender.SendAsync(
            email,
            htmlBody,
            textBody,
            cancellationToken);

        // Assert
        result.Should().Be("ses-message-123");
        capturedMimeContent.Should().NotBeNull();

        using var mimeStream = new MemoryStream(capturedMimeContent!);
        var mimeMessage = await MimeKit.MimeMessage.LoadAsync(
            mimeStream,
            cancellationToken);

        mimeMessage.From.Mailboxes.Should().ContainSingle()
            .Which.Address.Should().Be("noreply@example.com");

        mimeMessage.From.Mailboxes.Single().Name.Should().Be("Portfolio Studio");

        mimeMessage.To.Mailboxes.Should().ContainSingle()
            .Which.Address.Should().Be("recipient@example.com");

        mimeMessage.Subject.Should().Be("Welcome to Portfolio Studio");

        mimeMessage.Body.Should().BeAssignableTo<MimeKit.Multipart>();

        var body = (MimeKit.Multipart)mimeMessage.Body!;

        body.OfType<MimeKit.TextPart>()
            .Should().ContainSingle(x => x.IsHtml)
            .Which.Text.Should().Be(htmlBody);

        body.OfType<MimeKit.TextPart>()
            .Should().ContainSingle(x => !x.IsHtml)
            .Which.Text.Should().Be(textBody);

        ses.Verify(
            x => x.SendEmailAsync(
                It.IsAny<SendEmailRequest>(),
                cancellationToken),
            Times.Once);
    }
}