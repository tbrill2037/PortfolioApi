namespace Portfolio.Application.Common.Exceptions.Email;

/// <summary>
/// Represents an exception thrown when an email template is invalid.
/// </summary>
public sealed class InvalidEmailTemplateException : EmailException
{
    public InvalidEmailTemplateException(string templateName, Exception innerException)
        : base($"The email template '{templateName}' is invalid.", innerException)
    {
    }
}