using Portfolio.Application.Common.Interfaces.Email;
using System.Reflection;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Loads embedded email templates from the infrastructure assembly.
/// </summary>
public sealed class EmbeddedEmailTemplateLoader : IEmailTemplateLoader
{
    private const string ResourceRoot = "Portfolio.Infra.EmailTemplates";

    private readonly Assembly _assembly;

    public EmbeddedEmailTemplateLoader()
    {
        _assembly = typeof(EmbeddedEmailTemplateLoader).Assembly;
    }

    /// <summary>
    /// Loads an embedded email template by its resource path.
    /// </summary>
    public async Task<string> LoadAsync(string templatePath, CancellationToken cancellationToken = default)
    {
        var resourceName = $"{ResourceRoot}.{templatePath.Replace("/", ".")}";

        await using var stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new FileNotFoundException($"Embedded template not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync(cancellationToken);
    }
}