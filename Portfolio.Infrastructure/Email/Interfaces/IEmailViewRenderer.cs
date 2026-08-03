namespace Portfolio.Infra.Email.Interfaces;

/// <summary>
/// Defines email template rendering operations.
/// </summary>
public interface IEmailViewRenderer
{
    /// <summary>
    /// Renders a full email template with the provided model.
    /// </summary>
    Task<string> RenderAsync(
        string templatePath,
        object model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a partial email template with the provided model.
    /// </summary>
    Task<string> RenderPartialAsync(
        string templatePath,
        object model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders an email layout using the provided content sections.
    /// </summary>
    Task<string> RenderLayoutAsync(
        string bodyContent,
        string header,
        string footer,
        CancellationToken cancellationToken = default);
}