using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents an email resource used during message composition or delivery.
/// </summary>
public sealed class EmailResource
{
    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    /// <summary>
    /// The identifier used by the registered provider to locate the resource.
    /// </summary>
    public required string ResourceKey { get; init; }

    public EmailResourceType Type { get; init; }

    public string? ContentId { get; init; }

    public bool IsInline { get; init; }
}