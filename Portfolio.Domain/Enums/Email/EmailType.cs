namespace Portfolio.Domain.Enums.Email;

/// <summary>
/// Defines the types of emails supported by the application.
/// </summary>
public enum EmailType
{
    Unknown = 0,
    Contact = 1,
    PasswordReset = 2,
    Verification = 3,
}