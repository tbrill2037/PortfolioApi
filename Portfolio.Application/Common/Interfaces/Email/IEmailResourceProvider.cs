using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Provides access to email resources from a specific resource source.
/// </summary>
public interface IEmailResourceProvider
{
    /// <summary>
    /// Gets the resource type supported by this provider.
    /// </summary>
    EmailResourceType ResourceType { get; }

    /// <summary>
    /// Retrieves the content for the specified email resource.
    /// </summary>
    /// <param name="resource">The email resource to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The resource content as a byte array.</returns>
    Task<byte[]> GetAsync(EmailResource resource, CancellationToken cancellationToken = default);
}