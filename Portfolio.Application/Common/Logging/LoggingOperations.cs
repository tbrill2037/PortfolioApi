namespace Portfolio.Application.Common.Logging;

/// <summary>
/// Defines standardized operation names used for structured logging scopes and telemetry.
/// </summary>
public static class LoggingOperations
{
    public const string QueueEmail = nameof(QueueEmail);
    public const string ProcessEmail = nameof(ProcessEmail);
    public const string ProcessDeadLetter = nameof(ProcessDeadLetter);
    public const string ProcessEmailEvent = nameof(ProcessEmailEvent);
    public const string SendEmail = nameof(SendEmail);
}