namespace Portfolio.Domain.Entities;

/// <summary>
/// Represents an email message waiting to be published to the dispatch queue.
/// </summary>
public sealed class EmailOutboxEntity
{
    public Guid Id { get; set; }

    public Guid EmailMessageId { get; set; }

    public required string PayloadJson { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ProcessedUtc { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ProcessingLockUntilUtc { get; set; }

    public Guid? ProcessingLockToken { get; set; }

    public DateTime? ProcessingUtc { get; set; }
}