using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Resolves email resources by selecting the registered provider for each resource type.
/// </summary>
public sealed class EmailResourceResolver : IEmailResourceResolver
{
    private readonly Dictionary<EmailResourceType, IEmailResourceProvider> _providers;

    public EmailResourceResolver(IEnumerable<IEmailResourceProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.ResourceType);
    }

    /// <summary>
    /// Resolves an email resource using the provider registered for its resource type.
    /// </summary>
    public Task<byte[]> ResolveAsync(EmailResource resource, CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(resource.Type, out var provider))
        {
            throw new NotSupportedException($"No provider registered for {resource.Type}");
        }

        return provider.GetAsync(resource, cancellationToken);
    }
}