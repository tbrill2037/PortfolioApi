namespace Portfolio.Application.Common.Logging;

/// <summary>
/// Defines standardized queue type names used throughout the email processing pipeline.
/// </summary>
public static class QueueTypes
{
    public const string EmailDispatch = "EmailDispatch";
    public const string EmailEvents = "EmailEvents";
    public const string EmailDeadLetter = "EmailDeadLetter";
    public const string EmailOutbox = "EmailOutbox";
}