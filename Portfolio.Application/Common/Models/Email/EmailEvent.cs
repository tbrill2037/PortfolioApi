namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents an email event received from Amazon SES through Amazon SNS.
/// </summary>
public sealed class EmailEvent
{
    public required SesEvent SesEvent { get; init; }

    public required string RawPayload { get; init; }

    public required string SnsMessageId { get; init; }

    public required string TopicArn { get; init; }

    public DateTime ReceivedUtc { get; init; }
}