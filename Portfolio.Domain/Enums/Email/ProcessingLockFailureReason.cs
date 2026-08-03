namespace Portfolio.Domain.Enums.Email;

/// <summary>
/// Defines the possible reasons why an email processing lock could not be acquired.
/// </summary>
public enum ProcessingLockFailureReason
{
    None = 0,
    AlreadyLocked = 1,
    AlreadyCompleted = 2,
    NotFound = 3
}