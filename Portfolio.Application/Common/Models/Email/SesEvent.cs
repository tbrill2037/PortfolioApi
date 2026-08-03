using System.Text.Json.Serialization;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents an Amazon SES event notification received through Amazon SNS.
/// </summary>
public sealed class SesEvent
{
    [JsonPropertyName("EventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("Mail")]
    public SesMail Mail { get; set; } = new();
}

/// <summary>
/// Represents the mail metadata contained within an Amazon SES event.
/// </summary>
public sealed class SesMail
{
    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; } = string.Empty;
}