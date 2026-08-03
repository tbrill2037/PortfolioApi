using MediatR;

namespace Portfolio.Application.Commands.User.Delete;

/// <summary>
/// Represents a request to delete an existing application user.
/// </summary>
public sealed class DeleteUserCommand : IRequest<int>
{
    public required string Id { get; set; }
}