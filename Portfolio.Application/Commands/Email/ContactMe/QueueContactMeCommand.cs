using MediatR;

namespace Portfolio.Application.Commands.Email.ContactMe;

/// <summary>
/// Represents a request to queue a Contact Me email for asynchronous processing.
/// </summary>
public sealed record QueueContactMeCommand : IRequest
{
    /// <summary>
    /// Name of the person submitting the message.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Email address that can be used to reply.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Message submitted by the portfolio visitor.
    /// </summary>
    public required string Message { get; init; }
}