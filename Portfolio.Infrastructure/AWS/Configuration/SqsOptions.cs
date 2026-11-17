namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Defines configuration options for Amazon SQS queue processing.
/// </summary>
public sealed class SqsOptions
{
    public const string SectionName = "AWS:SQS";

    public required int MaxEmailRetries { get; init; }
    public required int MaxDLQRetries { get; init; }
    public required int MaxEventRetries { get; init; }

    public required string OutboxQueueUrl { get; init; }
    public required string OutboxDeadLetterQueueUrl { get; init; }
    public required string EmailQueueUrl { get; init; }
    public required string EmailDeadLetterQueueUrl { get; init; }
    public required string EmailEventQueueUrl { get; init; }
}