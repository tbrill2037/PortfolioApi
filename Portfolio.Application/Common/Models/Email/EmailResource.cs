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
    /// Where the resource is stored.
    /// Examples:
    /// embedded:Portfolio.Infra.EmailAssets.logo.png
    /// s3:emails/12345/resume.pdf
    /// </summary>
    public required string ResourceKey { get; init; }

    public EmailResourceType Type { get; init; }

    public string? ContentId { get; init; }

    public bool IsInline { get; init; }
}