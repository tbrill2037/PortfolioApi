namespace Portfolio.Application.Common.Exceptions.Email;

/// <summary>
/// Represents an exception thrown when an email template cannot be found.
/// </summary>
public sealed class EmailTemplateNotFoundException : EmailException
{
    public string TemplatePath { get; }

    public EmailTemplateNotFoundException(string templatePath, Exception innerException)
        : base($"Email template '{templatePath}' was not found.", innerException)
    {
        TemplatePath = templatePath;
    }
}