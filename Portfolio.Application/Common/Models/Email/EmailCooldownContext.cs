using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the context used when evaluating email request cooldown rules.
/// </summary>
public sealed class EmailCooldownContext
{
    public string? UserId { get; set; }

    public string? EmailAddress { get; set; }

    public string? IpAddress { get; set; }

    public EmailType EmailType { get; set; }
}