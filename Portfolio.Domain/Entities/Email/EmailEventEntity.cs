using Portfolio.Domain.Enums.Email;

namespace Portfolio.Domain.Entities.Email;

/// <summary>
/// Represents an event received from an email delivery provider.
/// </summary>
public sealed class EmailEventEntity
{
    public Guid Id { get; set; }

    public Guid EmailMessageId { get; set; }

    public EmailMessageEntity? EmailMessage { get; set; }

    /// <summary>
    /// The Amazon SES message identifier associated with this event.
    /// </summary>
    public required string SesMessageId { get; set; }

    public string? SnsMessageId { get; set; }

    public string? TopicArn { get; set; }

    public EmailEventType EventType { get; set; }

    public required string PayloadJson { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ReceivedUtc { get; set; }
}