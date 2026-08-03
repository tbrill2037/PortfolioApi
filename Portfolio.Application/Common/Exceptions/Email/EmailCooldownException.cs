using Portfolio.Application.Common.Exceptions.Email;

namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents an exception thrown when an email request is blocked due to a cooldown period.
/// </summary>
public sealed class EmailCooldownException : EmailException
{
    public EmailCooldownException(TimeSpan remaining)
        : base("Email request is temporarily blocked. Please try again later.")
    {
        Remaining = remaining;
    }

    public TimeSpan Remaining { get; }
}