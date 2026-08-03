namespace Portfolio.Application.Common.Exceptions.Email;

/// <summary>
/// Represents an exception thrown when the model supplied for an email template is invalid.
/// </summary>
public sealed class InvalidTemplateModelException : EmailException
{
    public InvalidTemplateModelException(string templateName)
        : base($"The model supplied for '{templateName}' is invalid.")
    {
    }
}