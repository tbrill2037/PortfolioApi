
namespace Portfolio.Infra.Email.Configuration;

/// <summary>
/// Defines configuration options for email processing and retry policies.
/// </summary>
public sealed class EmailProcessingOptions
{
    public const string SectionName = "Email:Processing";

    public required int ApplicationMaxEmailsPerSecond { get; init; }
    public required int ApplicationRateLimitQueueLimit { get; init; }
    public required int MaxOutboxRetries { get; init; }
    public required int MaxEmailRetries { get; init; }
    public required int MaxDLQRetries { get; init; }
    public required int MaxEventRetries { get; init; }
}
