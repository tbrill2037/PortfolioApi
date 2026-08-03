using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents an email message and its metadata throughout the email processing workflow.
/// </summary>
public sealed class EmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CorrelationId { get; set; }

    public Guid? CampaignId { get; set; }

    public required EmailAddress RecipientEmail { get; set; }

    public required EmailAddress SenderEmail { get; set; }

    public required string Subject { get; set; }

    public EmailType TemplateType { get; set; }

    public required string TemplatePath { get; set; }

    public object? TemplateModel { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public required IReadOnlyCollection<EmailResource> Resources { get; set; }
}