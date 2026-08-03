using Portfolio.Application.Common.Interfaces.Email;
using System.Collections.Concurrent;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Caches loaded email templates to avoid repeatedly reading embedded template resources.
/// </summary>
public sealed class CachedEmailTemplateLoader : IEmailTemplateLoader
{
    private readonly IEmailTemplateLoader _inner;
    private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _cache = new();

    public CachedEmailTemplateLoader(IEmailTemplateLoader inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Loads an email template from cache or the underlying template loader.
    /// </summary>
    public Task<string> LoadAsync(string templatePath, CancellationToken cancellationToken = default)
    {
        var cachedTemplate = _cache.GetOrAdd(
            templatePath,
            path => new Lazy<Task<string>>(
                () => _inner.LoadAsync(path, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return cachedTemplate.Value;
    }
}