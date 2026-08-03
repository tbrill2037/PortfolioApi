using Portfolio.Application.Common.Models.Email;
using Portfolio.Infra.Email.Configuration;

namespace Portfolio.Infra.Email.Models;

/// <summary>
/// Represents the model passed to Scriban when rendering an email template.
/// </summary>
public sealed class EmailTemplateContext
{
    /// <summary>
    /// Represents email metadata and delivery information.
    /// </summary>
    public required EmailMessage Message { get; init; }

    /// <summary>
    /// Represents the strongly typed template-specific email model.
    /// </summary>
    public required IEmailTemplateModel TemplateModel { get; init; }

    /// <summary>
    /// Represents shared email branding configuration.
    /// </summary>
    public required EmailBrandingOptions Branding { get; init; }
}