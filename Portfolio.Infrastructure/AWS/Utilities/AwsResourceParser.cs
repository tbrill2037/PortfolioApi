namespace Portfolio.Infra.AWS.Utilities;

/// <summary>
/// Provides utility methods for parsing AWS resource identifiers.
/// </summary>
public static class AwsResourceParser
{
    /// <summary>
    /// Extracts the resource name from an AWS queue URL.
    /// </summary>
    public static string GetQueueNameFromUrl(string queueUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueUrl);

        var uri = new Uri(queueUrl);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length == 0 ? string.Empty : segments[^1];
    }
}