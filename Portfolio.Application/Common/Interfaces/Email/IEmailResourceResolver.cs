using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Resolves email resources using the appropriate resource provider.
/// </summary>
public interface IEmailResourceResolver
{
    /// <summary>
    /// Resolves the specified email resource.
    /// </summary>
    /// <param name="resource">The email resource to resolve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The resolved resource content as a byte array.</returns>
    Task<byte[]> ResolveAsync(EmailResource resource, CancellationToken cancellationToken = default);
}