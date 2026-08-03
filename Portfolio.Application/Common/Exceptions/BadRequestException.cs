namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents an exception thrown when a request contains invalid data or cannot be processed.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException()
        : base()
    {
    }

    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}