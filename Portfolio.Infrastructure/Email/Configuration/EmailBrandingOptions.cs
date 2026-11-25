namespace Portfolio.Infra.Email.Configuration;

/// <summary>
/// Defines branding settings used when composing email templates.
/// </summary>
public sealed class EmailBrandingOptions
{
    public const string SectionName = "EmailBranding";

    public required string Name { get; init; }
    public required string Tagline { get; init; }
    public required string WebsiteUrl { get; init; }
    public required string GitHubUrl { get; init; }
    public required string LinkedInUrl { get; init; }
    public required string StackOverflowUrl { get; init; }
}