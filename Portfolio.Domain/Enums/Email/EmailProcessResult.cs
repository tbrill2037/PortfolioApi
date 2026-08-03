namespace Portfolio.Domain.Enums.Email;

/// <summary>
/// Defines the possible outcomes of an email processing operation.
/// </summary>
public enum EmailProcessResult
{
    Unknown = 0,
    Sent = 1,
    AlreadyProcessed = 2,
    NotFound = 3,
    Locked = 4,
    RetryableFailure = 5,
    PermanentFailure = 6,
}