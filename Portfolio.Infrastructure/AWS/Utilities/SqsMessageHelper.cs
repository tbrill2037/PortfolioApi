using Amazon.Lambda.SQSEvents;

namespace Portfolio.Application.Common.AWS;

/// <summary>
/// Provides helper methods for extracting SQS message metadata from AWS Lambda queue events.
/// </summary>
public static class SqsMessageHelper
{
    /// <summary>
    /// Gets the number of times an SQS message has been received.
    /// </summary>
    public static int GetReceiveCount(SQSEvent.SQSMessage record)
    {
        if (record.Attributes is null)
        {
            return 1;
        }

        if (record.Attributes.TryGetValue("ApproximateReceiveCount", out var receiveCount) &&
            int.TryParse(receiveCount, out var count))
        {
            return count;
        }

        return 1;
    }

    /// <summary>
    /// Gets the timestamp when an SQS message was sent.
    /// </summary>
    public static DateTimeOffset? GetSentTimestamp(SQSEvent.SQSMessage record)
    {
        if (record.Attributes?.TryGetValue("SentTimestamp", out var timestamp) == true &&
            long.TryParse(timestamp, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }

        return null;
    }

    /// <summary>
    /// Gets the timestamp when an SQS message was first received.
    /// </summary>
    public static DateTimeOffset? GetFirstReceiveTimestamp(SQSEvent.SQSMessage record)
    {
        if (record.Attributes?.TryGetValue("ApproximateFirstReceiveTimestamp", out var timestamp) == true &&
            long.TryParse(timestamp, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }

        return null;
    }
}