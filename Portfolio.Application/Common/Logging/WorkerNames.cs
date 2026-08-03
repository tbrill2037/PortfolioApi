namespace Portfolio.Application.Common.Logging;

/// <summary>
/// Defines standardized worker names used for structured logging and telemetry.
/// </summary>
public static class WorkerNames
{
    public const string API = "API";
    public const string EmailWorker = "EmailWorker";
    public const string EmailEventWorker = "EmailEventWorker";
    public const string EmailDlqWorker = "EmailDlqWorker";
    public const string EmailOutboxWorker = "EmailOutboxWorker";
}