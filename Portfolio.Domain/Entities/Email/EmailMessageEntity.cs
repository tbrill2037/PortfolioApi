using Portfolio.Domain.Enums.Email;

namespace Portfolio.Domain.Entities.Email;

/// <summary>
/// Represents an email message and its processing lifecycle.
/// </summary>
public sealed class EmailMessageEntity
{
    public Guid Id { get; set; }

    public Guid CorrelationId { get; set; }

    public Guid? CampaignId { get; set; }

    public required string Recipient { get; set; }

    public required string RecipientName { get; set; }

    public required string Sender { get; set; }

    public required string SenderName { get; set; }

    public required string Subject { get; set; }

    public EmailType TemplateType { get; set; }

    public required string TemplatePath { get; set; }

    public EmailStatus Status { get; set; }

    public string? SesMessageId { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime? LastAttemptUtc { get; set; }

    public DateTime? LastErrorUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ProcessingLockUntilUtc { get; set; }

    public Guid? ProcessingLockToken { get; set; }

    public DateTime? ProcessingUtc { get; set; }

    public DateTime? SentUtc { get; set; }

    public DateTime? DeadLetteredUtc { get; set; }

    public ICollection<EmailEventEntity> Events { get; set; } = new List<EmailEventEntity>();
}