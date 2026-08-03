using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Exceptions.Email;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Application.Common.Serialization;
using Portfolio.Infra.Email.Configuration;
using Portfolio.Infra.Email.Interfaces;
using Portfolio.Infra.Email.Models;
using System.Text.Json;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Composes email content by rendering templates, layouts, and branding components.
/// </summary>
public sealed class EmailComposer : IEmailComposer
{
    private readonly EmailBrandingOptions _brandingOptions;
    private readonly IEmailViewRenderer _renderer;
    private readonly IEmailTemplateModelResolver _templateResolver;

    public EmailComposer(
        IOptions<EmailBrandingOptions> brandingOptions,
        IEmailViewRenderer renderer,
        IEmailTemplateModelResolver templateResolver)
    {
        _brandingOptions = brandingOptions.Value;
        _renderer = renderer;
        _templateResolver = templateResolver;
    }

    /// <summary>
    /// Composes HTML and plain-text email content from a strongly typed template model.
    /// </summary>
    public async Task<EmailContent> ComposeAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message.TemplateModel is null)
        {
            throw new InvalidTemplateModelException(message.TemplateType.ToString());
        }

        var modelType = _templateResolver.Resolve(message.TemplateType.ToString());

        if (!typeof(IEmailTemplateModel).IsAssignableFrom(modelType))
        {
            throw new InvalidTemplateModelException(message.TemplateType.ToString());
        }

        IEmailTemplateModel templateModel;

        if (message.TemplateModel is JsonElement json)
        {
            templateModel =
                JsonSerializer.Deserialize(json.GetRawText(), modelType, JsonSerializerOptionsDefaults.Default)
                as IEmailTemplateModel
                ?? throw new InvalidTemplateModelException(message.TemplateType.ToString());
        }
        else if (message.TemplateModel is IEmailTemplateModel existingModel)
        {
            templateModel = existingModel;
        }
        else
        {
            throw new InvalidTemplateModelException(message.TemplateType.ToString());
        }

        var context = new EmailTemplateContext
        {
            Message = message,
            TemplateModel = templateModel,
            Branding = _brandingOptions
        };

        try
        {
            var body = await _renderer.RenderAsync(
                message.TemplatePath,
                context,
                cancellationToken);

            var header = await _renderer.RenderPartialAsync(
                EmailTemplatePaths.GetHeader(message.TemplateType),
                context,
                cancellationToken);

            var footer = await _renderer.RenderPartialAsync(
                EmailTemplatePaths.GetFooter(message.TemplateType),
                context,
                cancellationToken);

            var html = await _renderer.RenderLayoutAsync(
                body,
                header,
                footer,
                cancellationToken);

            return new EmailContent
            {
                HtmlBody = html,
                TextBody = GeneratePlainText(html)
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidEmailTemplateException(message.TemplateType.ToString(), ex);
        }
    }

    /// <summary>
    /// Generates a basic plain-text representation from rendered HTML.
    /// </summary>
    private static string GeneratePlainText(string html)
    {
        return html
            .Replace("<br>", "\n")
            .Replace("<p>", "")
            .Replace("</p>", "\n");
    }
}