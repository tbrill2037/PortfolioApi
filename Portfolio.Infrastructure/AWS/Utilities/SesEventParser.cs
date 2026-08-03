using System.Text.Json;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Application.Common.Serialization;

namespace Portfolio.Application.Common.Parsing.Email;

/// <summary>
/// Parses SNS notifications containing Amazon SES event payloads.
/// </summary>
public sealed class SesEventParser : ISesEventParser
{
    public EmailEvent Parse(string snsPayload)
    {
        var snsEvent = JsonSerializer.Deserialize<SnsNotification>(
            snsPayload,
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
            SesEvent = sesEvent,
            RawPayload = snsEvent.Message,
            SnsMessageId = snsEvent.MessageId,
            TopicArn = snsEvent.TopicArn,
            ReceivedUtc = snsEvent.Timestamp
        };
    }
}