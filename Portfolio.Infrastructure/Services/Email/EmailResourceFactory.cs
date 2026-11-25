using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Creates email resource definitions for embedded assets and S3-backed resources.
/// </summary>
public static class EmailResourceFactory
{
    private const string ResourceRoot = "Portfolio.Infra.EmailAssets";

    /// <summary>
    /// Creates an embedded email resource loaded from application assembly assets.
    /// </summary>
    public static EmailResource CreateEmbedded(
        string fileName,
        string contentId)
    {
        return new EmailResource
        {
            FileName = fileName,
            ContentType = GetContentType(fileName),
            ResourceKey = $"{ResourceRoot}.{fileName}",
            Type = EmailResourceType.Embedded,
            ContentId = contentId,
            IsInline = true
        };
    }

    /// <summary>
    /// Creates an externally stored email resource definition.
    /// </summary>
    public static EmailResource CreateExternal(
        string fileName,
        string contentType,
        string key,
        bool isInline = false,
        string? contentId = null)
    {
        return new EmailResource
        {
            FileName = fileName,
            ContentType = contentType,
            ResourceKey = key,
            Type = EmailResourceType.External,
            ContentId = contentId,
            IsInline = isInline
        };
    }

    /// <summary>
    /// Resolves the MIME content type for supported email asset extensions.
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => throw new NotSupportedException($"Unsupported email resource type: {fileName}")
        };
    }
}