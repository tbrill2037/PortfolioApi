namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Defines configuration options for AWS Lambda execution behavior.
/// </summary>
public sealed class LambdaOptions
{
    public const string SectionName = "AWS:Lambda";

    public required string DevelopmentFallbackTime { get; init; }
    public required string EmailOutboxProcessingBuffer { get; init; }
    public required string EmailProcessingBuffer { get; init; }
    public required string EventProcessingBuffer { get; init; }
    public required string DlqProcessingBuffer { get; init; }

    public TimeSpan DevelopmentFallback =>
        TimeSpan.Parse(DevelopmentFallbackTime);

    public TimeSpan EmailOutboxProcessorBuffer =>
        TimeSpan.Parse(EmailOutboxProcessingBuffer);

    public TimeSpan EmailProcessorBuffer =>
        TimeSpan.Parse(EmailProcessingBuffer);

    public TimeSpan EmailEventProcessorBuffer =>
        TimeSpan.Parse(EventProcessingBuffer);

    public TimeSpan DlqProcessorBuffer =>
        TimeSpan.Parse(DlqProcessingBuffer);
}