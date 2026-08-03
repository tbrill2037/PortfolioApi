namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents an exception thrown when a request conflicts with the current application state.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}