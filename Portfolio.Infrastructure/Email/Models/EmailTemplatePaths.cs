using Portfolio.Domain.Enums.Email;

namespace Portfolio.Infra.Email.Models;

/// <summary>
/// Defines the embedded template paths and shared layouts used for email rendering.
/// </summary>
public static class EmailTemplatePaths
{
    public const string Layout = "Shared/Layouts/Default.html";
    public const string DefaultHeader = "Shared/Headers/Default.html";
    public const string DefaultFooter = "Shared/Footers/Default.html";
    public const string PortfolioHeader = "Shared/Headers/Portfolio.html";
    public const string PortfolioFooter = "Shared/Footers/Portfolio.html";
    public const string ContactMe = "Contact/Contact.v1.html";

    /// <summary>
    /// Gets the header template for the specified email type.
    /// </summary>
    public static string GetHeader(EmailType emailType)
    {
        return emailType switch
        {
            EmailType.ContactMe => PortfolioHeader,
            _ => DefaultHeader
        };
    }

    /// <summary>
    /// Gets the footer template for the specified email type.
    /// </summary>
    public static string GetFooter(EmailType emailType)
    {
        return emailType switch
        {
            EmailType.ContactMe => PortfolioFooter,
            _ => DefaultFooter
        };
    }
}