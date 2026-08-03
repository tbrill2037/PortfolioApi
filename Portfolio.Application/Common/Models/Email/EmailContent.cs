namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the generated email content containing HTML and plain text formats.
/// </summary>
public sealed class EmailContent
{
    public required string HtmlBody { get; init; }

    public required string TextBody { get; init; }
}