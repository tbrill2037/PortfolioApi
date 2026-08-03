using Portfolio.Infra.Email.Interfaces;
using Portfolio.Infra.Email.Models;
using Scriban;
using Scriban.Runtime;
using System.Reflection;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Renders Scriban email templates using embedded template resources and supplied models.
/// </summary>
public sealed class ScribanViewRenderer : IEmailViewRenderer
{
    private const string ResourceRoot = "Portfolio.Infra.EmailTemplates";

    private readonly Assembly _assembly;

    public ScribanViewRenderer()
    {
        _assembly = typeof(ScribanViewRenderer).Assembly;
    }


    /// <summary>
    /// Renders a full email template.
    /// </summary>
    public async Task<string> RenderAsync(
        string templatePath,
        object model,
        CancellationToken cancellationToken)
    {
        var template =
            await LoadTemplateAsync(
                templatePath,
                cancellationToken);

        var context =
            CreateTemplateContext(model);

        return await template.RenderAsync(context);
    }


    /// <summary>
    /// Renders a partial email template.
    /// </summary>
    public async Task<string> RenderPartialAsync(
        string templatePath,
        object model,
        CancellationToken cancellationToken)
    {
        var template =
            await LoadTemplateAsync(
                templatePath,
                cancellationToken);

        var context =
            CreateTemplateContext(model);

        return await template.RenderAsync(context);
    }


    /// <summary>
    /// Renders the shared email layout using rendered header,
    /// body, and footer content.
    /// </summary>
    public async Task<string> RenderLayoutAsync(
        string bodyContent,
        string header,
        string footer,
        CancellationToken cancellationToken = default)
    {
        var layoutTemplate =
            await LoadTemplateAsync(
                EmailTemplatePaths.Layout,
                cancellationToken);

        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name
        };

        var scriptObject = new ScriptObject
        {
            ["Header"] = header,
            ["BodyContent"] = bodyContent,
            ["Footer"] = footer
        };

        context.PushGlobal(scriptObject);

        return await layoutTemplate.RenderAsync(context);
    }


    private static TemplateContext CreateTemplateContext(
        object model)
    {
        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name
        };

        var scriptObject = new ScriptObject();


        if (model is EmailTemplateContext emailContext)
        {
            var message = new ScriptObject();

            message.Import(
                emailContext.Message,
                renamer: member => member.Name);

            scriptObject["Message"] = message;


            scriptObject["TemplateModel"] =
                ScribanTemplateModelFactory.Create(
                    emailContext.TemplateModel);


            var branding = new ScriptObject();

            branding.Import(
                emailContext.Branding,
                renamer: member => member.Name);

            scriptObject["Branding"] = branding;
        }
        else
        {
            scriptObject.Import(
                model,
                renamer: member => member.Name);
        }


        context.PushGlobal(scriptObject);

        return context;
    }


    private async Task<Template> LoadTemplateAsync(
        string templatePath,
        CancellationToken cancellationToken)
    {
        var text =
            await LoadTextAsync(
                templatePath,
                cancellationToken);

        var template =
            Template.Parse(text);

        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                string.Join(
                    Environment.NewLine,
                    template.Messages));
        }

        return template;
    }


    private async Task<string> LoadTextAsync(
        string templatePath,
        CancellationToken cancellationToken)
    {
        var resourceName =
            $"{ResourceRoot}.{templatePath.Replace("/", ".")}";

        await using var stream =
            _assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new FileNotFoundException(
                $"Embedded email template not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync(cancellationToken);
    }
}