namespace Portfolio.Domain.Entities.Email;

/// <summary>
/// Represents an email request used for tracking cooldown enforcement.
/// </summary>
public sealed class EmailRequestEntity
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    public string? EmailAddress { get; set; }

    public string? IpAddress { get; set; }

    public required string EmailType { get; set; }

    public DateTime RequestedUtc { get; set; }
}