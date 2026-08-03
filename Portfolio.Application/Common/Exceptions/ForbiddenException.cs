namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents an exception thrown when a user does not have permission to perform an action.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}