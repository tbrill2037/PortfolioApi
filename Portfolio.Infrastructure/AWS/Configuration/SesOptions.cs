namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Defines configuration options for Amazon SES email settings.
/// </summary>
public sealed class SesOptions
{
    public const string SectionName = "AWS:SES";

    public required string FromEmail { get; init; }
    public required string FromName { get; init; }

    /// <summary>
    /// SES configuration set used when sending emails.
    /// </summary>
    public required string ConfigurationSet { get; init; }

    /// <summary>
    /// SNS topic ARN receiving SES delivery events.
    /// </summary>
    public required string EventTopicArn { get; init; }

    /// <summary>
    /// Maximum emails allowed per second before throttling protection applies.
    /// </summary>
    public int MaxEmailsPerSecond { get; init; }

    /// <summary>
    /// Minimum SES quota threshold that triggers monitoring warnings.
    /// </summary>
    public int LowQuotaThreshold { get; init; }
}