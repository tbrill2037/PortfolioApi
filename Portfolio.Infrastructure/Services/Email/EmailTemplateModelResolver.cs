using Portfolio.Application.Common.Exceptions.Email;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Resolves the CLR model type associated with an email template.
/// </summary>
public sealed class EmailTemplateModelResolver : IEmailTemplateModelResolver
{
    /// <summary>
    /// Resolves the model type for the specified email template name.
    /// </summary>
    public Type Resolve(string templateType)
    {
        return templateType switch
        {
            nameof(EmailType.ContactMe) =>
                typeof(ContactMeEmailModel),

            _ => throw new InvalidTemplateModelException(templateType)
        };
    }
}