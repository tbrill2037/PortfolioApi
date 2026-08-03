namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Resolves the CLR model type associated with an email template.
/// </summary>
public interface IEmailTemplateModelResolver
{
    /// <summary>
    /// Resolves the model type for the specified email template.
    /// </summary>
    /// <param name="templateName">The name of the email template.</param>
    /// <returns>The model type associated with the template.</returns>
    Type Resolve(string templateName);
}