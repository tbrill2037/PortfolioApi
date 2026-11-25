using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents an exception thrown when an email message cannot be found.
/// </summary>
public sealed class EmailNotFoundException : NotFoundException
{
    public EmailNotFoundException(string providerMessageId)
        : base(nameof(EmailMessage), providerMessageId)
    {
    }
}