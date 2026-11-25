namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents a message received from an email dead-letter queue.
/// </summary>
public sealed record EmailDeadLetterMessage(
    string QueueMessageId,
    string Body,
    int ReceiveCount);
