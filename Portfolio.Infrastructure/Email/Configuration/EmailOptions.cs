namespace Portfolio.Infra.Email.Configuration;

/// <summary>
/// Defines provider-neutral email configuration options.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public required string FromName { get; init; }
    public required string FromEmail { get; init; }
}