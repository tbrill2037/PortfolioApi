using Amazon.Lambda.SQSEvents;

namespace Portfolio.Infra.AWS.Utilities;

/// <summary>
/// Provides helper methods for handling SQS batch processing failures.
/// </summary>
public static class SqsBatchFailureHelper
{
    /// <summary>
    /// Adds a failed SQS message identifier to the batch failure response.
    /// </summary>
    public static void AddFailure(ICollection<SQSBatchResponse.BatchItemFailure> failures, SQSEvent.SQSMessage record)
    {
        failures.Add(new SQSBatchResponse.BatchItemFailure
        {
            ItemIdentifier = record.MessageId,
        });
    }
}