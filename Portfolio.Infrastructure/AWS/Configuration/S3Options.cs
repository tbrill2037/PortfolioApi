namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Defines configuration options for AWS S3 resources.
/// </summary>
public sealed class S3Options
{
    public const string SectionName = "AWS:S3";

    public required string BucketName { get; init; }
}