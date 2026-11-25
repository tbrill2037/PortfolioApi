namespace Portfolio.Domain.Entities.Email;

/// <summary>
/// Represents an email message that has been moved to a dead-letter queue.
/// </summary>
public sealed class EmailDeadLetterEntity
{
    public Guid Id { get; set; }

    public Guid? EmailMessageId { get; set; }

    public required string QueueMessageId { get; set; }

    public required string Payload { get; set; }

    public string? FailureReason { get; set; }

    public string? ExceptionType { get; set; }

    public int ReceiveCount { get; set; }

    public DateTime CreatedUtc { get; set; }
}