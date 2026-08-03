namespace Portfolio.Application.Common.Exceptions.Email;

/// <summary>
/// Represents an exception that occurs when an email fails during the composition process.
/// </summary>
public sealed class EmailCompositionException : EmailException
{
    public EmailCompositionException(string templateName, Exception innerException)
        : base($"Failed to compose email '{templateName}'.", innerException)
    {
    }
}