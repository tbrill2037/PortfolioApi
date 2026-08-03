using System.Text.Json.Serialization;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents an Amazon SNS notification containing an email event.
/// </summary>
public sealed class SnsNotification
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("TopicArn")]
    public string TopicArn { get; set; } = string.Empty;

    [JsonPropertyName("Subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("SignatureVersion")]
    public string SignatureVersion { get; set; } = string.Empty;

    [JsonPropertyName("Signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("SigningCertURL")]
    public string SigningCertURL { get; set; } = string.Empty;

    [JsonPropertyName("UnsubscribeURL")]
    public string UnsubscribeURL { get; set; } = string.Empty;
}