namespace Portfolio.Application.Common.Exceptions.Email;

/// <summary>
/// Represents the base exception type for email-related application errors.
/// </summary>
public abstract class EmailException : Exception
{
    protected EmailException()
    {
    }

    protected EmailException(string message)
        : base(message)
    {
    }

    protected EmailException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}