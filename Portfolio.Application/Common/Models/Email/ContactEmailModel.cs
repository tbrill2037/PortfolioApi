namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the template data used when rendering a Contact Me email.
///
/// This model is specifically for email presentation.
/// It should not be reused as an API request DTO.
/// </summary>
public sealed class ContactEmailModel : IEmailTemplateModel
{
    /// <summary>
    /// Name submitted by the visitor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Email address submitted by the visitor.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Message submitted by the visitor.
    /// </summary>
    public required string Message { get; init; }
}