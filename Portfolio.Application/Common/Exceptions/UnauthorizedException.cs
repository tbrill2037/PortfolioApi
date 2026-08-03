namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents an exception thrown when authentication fails or access is denied.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}