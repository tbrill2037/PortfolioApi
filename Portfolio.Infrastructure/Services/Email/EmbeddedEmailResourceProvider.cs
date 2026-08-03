using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Provides access to embedded email resources stored in the application assembly.
/// </summary>
public sealed class EmbeddedEmailResourceProvider : IEmailResourceProvider
{
    public EmailResourceType ResourceType => EmailResourceType.Embedded;

    /// <summary>
    /// Loads an embedded email resource from the application assembly.
    /// </summary>
    public async Task<byte[]> GetAsync(EmailResource resource, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(EmbeddedEmailResourceProvider).Assembly;

        await using var stream = assembly.GetManifestResourceStream(resource.ResourceKey)
            ?? throw new FileNotFoundException($"Embedded email resource not found: {resource.ResourceKey}");

        using var memory = new MemoryStream();

        await stream.CopyToAsync(memory, cancellationToken);

        return memory.ToArray();
    }
}