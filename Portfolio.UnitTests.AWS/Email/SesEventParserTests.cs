using FluentAssertions;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Services.Email;
using System.Text.Json;

namespace Portfolio.UnitTests.AWS.Email;

public sealed class SesEventParserTests
{
    [Fact]
    public void Parse_ShouldReturnNormalizedEmailEvent()
    {
        // Arrange
        var timestamp = new DateTime(2026, 11, 25, 15, 30, 0, DateTimeKind.Utc);

        const string sesPayload =
            """
            {
                "EventType": "Delivery",
                "Mail": {
                    "MessageId": "ses-message-123"
                }
            }
            """;

        var snsPayload = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-message-123",
            TopicArn = "arn:aws:sns:us-east-1:123456789012:TestEmailEvents",
            Subject = "Amazon SES Email Event",
            Message = sesPayload,
            Timestamp = timestamp,
            SignatureVersion = "1",
            Signature = "test-signature",
            SigningCertURL = "https://example.com/certificate.pem",
            UnsubscribeURL = "https://example.com/unsubscribe"
        });

        var parser = new SesEventParser();

        // Act
        var result = parser.Parse(snsPayload);

        // Assert
        result.ProviderMessageId.Should().Be("ses-message-123");
        result.EventType.Should().Be(EmailEventType.Delivery);
        result.RawPayload.Should().Be(sesPayload);
        result.ReceivedUtc.Should().Be(timestamp);
    }

    [Fact]
    public void Parse_ShouldThrowWhenSnsPayloadIsInvalid()
    {
        // Arrange
        const string payload = "{ invalid json }";

        var parser = new SesEventParser();

        // Act
        var act = () => parser.Parse(payload);

        // Assert
        act.Should()
            .Throw<JsonException>();
    }

    [Fact]
    public void Parse_ShouldThrowWhenSesPayloadIsInvalid()
    {
        // Arrange
        var snsPayload = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-message-123",
            Message = "{ invalid json }",
            Timestamp = DateTime.UtcNow
        });

        var parser = new SesEventParser();

        // Act
        var act = () => parser.Parse(snsPayload);

        // Assert
        act.Should()
            .Throw<JsonException>();
    }

    [Fact]
    public void Parse_ShouldThrowWhenSesMessageIdIsMissing()
    {
        // Arrange
        const string sesPayload =
            """
        {
            "EventType": "Delivery",
            "Mail": {
                "MessageId": ""
            }
        }
        """;

        var snsPayload = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-message-123",
            Message = sesPayload,
            Timestamp = DateTime.UtcNow
        });

        var parser = new SesEventParser();

        // Act
        var act = () => parser.Parse(snsPayload);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage("SES message ID is missing.");
    }

    [Fact]
    public void Parse_ShouldThrowWhenSesMessageIdIsWhitespace()
    {
        // Arrange
        const string sesPayload =
            """
        {
            "EventType": "Delivery",
            "Mail": {
                "MessageId": "   "
            }
        }
        """;

        var snsPayload = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-message-123",
            Message = sesPayload,
            Timestamp = DateTime.UtcNow
        });

        var parser = new SesEventParser();

        // Act
        var act = () => parser.Parse(snsPayload);

        // Assert
        act.Should()
            .Throw<JsonException>()
            .WithMessage("SES message ID is missing.");
    }

    [Fact]
    public void Parse_ShouldMapUnknownEventTypeToUnknown()
    {
        // Arrange
        const string sesPayload =
            """
        {
            "EventType": "UnknownEvent",
            "Mail": {
                "MessageId": "ses-message-123"
            }
        }
        """;

        var snsPayload = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-message-123",
            Message = sesPayload,
            Timestamp = DateTime.UtcNow
        });

        var parser = new SesEventParser();

        // Act
        var result = parser.Parse(snsPayload);

        // Assert
        result.EventType.Should().Be(EmailEventType.Unknown);
    }

    [Theory]
    [InlineData("Send", EmailEventType.Send)]
    [InlineData("Delivery", EmailEventType.Delivery)]
    [InlineData("Bounce", EmailEventType.Bounce)]
    [InlineData("Complaint", EmailEventType.Complaint)]
    [InlineData("Reject", EmailEventType.Reject)]
    [InlineData("RenderingFailure", EmailEventType.RenderingFailure)]
    [InlineData("Open", EmailEventType.Open)]
    [InlineData("Click", EmailEventType.Click)]
    [InlineData("DeliveryDelay", EmailEventType.DeliveryDelay)]
    [InlineData("Subscription", EmailEventType.Subscription)]
    public void Parse_ShouldMapSupportedEventTypes(
    string eventType,
    EmailEventType expectedEventType)
    {
        // Arrange
        var sesPayload = JsonSerializer.Serialize(new
        {
            EventType = eventType,
            Mail = new
            {
                MessageId = "ses-message-123"
            }
        });

        var snsPayload = JsonSerializer.Serialize(new
        {
            Type = "Notification",
            MessageId = "sns-message-123",
            Message = sesPayload,
            Timestamp = DateTime.UtcNow
        });

        var parser = new SesEventParser();

        // Act
        var result = parser.Parse(snsPayload);

        // Assert
        result.EventType.Should().Be(expectedEventType);
    }
}