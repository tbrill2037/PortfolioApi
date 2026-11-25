using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Application.Common.Serialization;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Models;
using System.Text.Json;

namespace Portfolio.Infra.AWS.Services.Email;

/// <summary>
/// Parses SNS notifications containing Amazon SES event payloads.
/// </summary>
public sealed class SesEventParser : IEmailEventParser
{
    public EmailEvent Parse(string payload)
    {
        var snsEvent = JsonSerializer.Deserialize<SnsNotification>(
            payload,
            JsonSerializerOptionsDefaults.Default)
            ?? throw new JsonException("SNS payload is invalid.");

        var sesEvent = JsonSerializer.Deserialize<SesEvent>(
            snsEvent.Message,
            JsonSerializerOptionsDefaults.Default)
            ?? throw new JsonException("SES payload is invalid.");

        if (string.IsNullOrWhiteSpace(sesEvent.Mail.MessageId))
        {
            throw new JsonException("SES message ID is missing.");
        }

        return new EmailEvent
        {
            ProviderMessageId = sesEvent.Mail.MessageId,
            TransportMessageId = string.IsNullOrWhiteSpace(snsEvent.MessageId)
                ? null
                : snsEvent.MessageId,
            TransportTopic = string.IsNullOrWhiteSpace(snsEvent.TopicArn)
                ? null
                : snsEvent.TopicArn,
            EventType = MapEventType(sesEvent.EventType),
            RawPayload = snsEvent.Message,
            ReceivedUtc = snsEvent.Timestamp
        };
    }

    private static EmailEventType MapEventType(string eventType)
    {
        return eventType switch
        {
            "Send" => EmailEventType.Send,
            "Delivery" => EmailEventType.Delivery,
            "Bounce" => EmailEventType.Bounce,
            "Complaint" => EmailEventType.Complaint,
            "Reject" => EmailEventType.Reject,
            "RenderingFailure" => EmailEventType.RenderingFailure,
            "Open" => EmailEventType.Open,
            "Click" => EmailEventType.Click,
            "DeliveryDelay" => EmailEventType.DeliveryDelay,
            "Subscription" => EmailEventType.Subscription,
            _ => EmailEventType.Unknown
        };
    }
}