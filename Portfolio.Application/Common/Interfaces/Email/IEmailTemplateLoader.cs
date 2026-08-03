namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Loads the content of an email template.
/// </summary>
public interface IEmailTemplateLoader
{
    /// <summary>
    /// Loads an email template from the configured template source.
    /// </summary>
    /// <param name="templatePath">The relative path to the email template.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The email template content.</returns>
    Task<string> LoadAsync(string templatePath, CancellationToken cancellationToken = default);
}