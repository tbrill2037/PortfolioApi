using Portfolio.Application.Common.Models.Email;
using Scriban.Runtime;
using System.Net;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Converts strongly typed email template models into Scriban script objects.
///
/// This class is the security boundary between application data
/// and HTML email templates. User-controlled values are HTML encoded
/// before being exposed to Scriban.
/// </summary>
public static class ScribanTemplateModelFactory
{
    /// <summary>
    /// Creates a safe Scriban object from an email template model.
    /// </summary>
    public static ScriptObject Create(
        IEmailTemplateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model switch
        {
            ContactMeEmailModel contact =>
                CreateContactMeModel(contact),

            _ => throw new InvalidOperationException(
                $"Unsupported email template model '{model.GetType().Name}'.")
        };
    }


    /// <summary>
    /// Creates the Scriban model for Contact Me emails.
    /// </summary>
    private static ScriptObject CreateContactMeModel(
        ContactMeEmailModel model)
    {
        return new ScriptObject
        {
            ["Name"] = Encode(model.Name),

            ["Email"] = Encode(model.Email),

            ["Message"] = Encode(model.Message)
        };
    }


    /// <summary>
    /// HTML encodes user supplied values before rendering.
    /// </summary>
    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}