namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Defines configuration options for Amazon SES email settings.
/// </summary>
public sealed class SesOptions
{
    public const string SectionName = "AWS:SES";

    public required string ConfigurationSet { get; init; }
    public required string EventTopicArn { get; init; }
    public int MaxEmailsPerSecond { get; init; }
    public int RateLimitQueueLimit { get; init; }
    public int LowQuotaThreshold { get; init; }
}