using MediatR;

namespace Portfolio.Application.Commands.Email.ContactMe;

/// <summary>
/// Represents a request to queue a Contact Me email for asynchronous processing.
/// </summary>
public sealed class QueueContactMeCommand : IRequest
{
    public required string Email { get; set; }

    public required string Name { get; set; }

    public required string Message { get; set; }
}