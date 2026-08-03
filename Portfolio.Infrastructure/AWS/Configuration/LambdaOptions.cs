namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Defines configuration options for AWS Lambda execution behavior.
/// </summary>
public sealed class LambdaOptions
{
    public const string SectionName = "AWS:Lambda";

    public required string DevelopmentFallbackTime { get; init; } = "00:01:00";
    public required string EmailOutboxProcessingBuffer { get; init; } = "00:00:10";
    public required string EmailProcessingBuffer { get; init; } = "00:00:30";
    public required string EventProcessingBuffer { get; init; } = "00:00:10";
    public required string DlqProcessingBuffer { get; init; } = "00:00:10";

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