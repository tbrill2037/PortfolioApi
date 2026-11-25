using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents a normalized email provider event.
/// </summary>
public sealed class EmailEvent
{
    public required string ProviderMessageId { get; init; }

    public string? TransportMessageId { get; init; }

    public string? TransportTopic { get; init; }

    public required EmailEventType EventType { get; init; }

    public required string RawPayload { get; init; }

    public DateTime ReceivedUtc { get; init; }
}