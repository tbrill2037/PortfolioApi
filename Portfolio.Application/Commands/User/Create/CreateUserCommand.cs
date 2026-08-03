using MediatR;

namespace Portfolio.Application.Commands.User.Create;

/// <summary>
/// Represents a request to create a new application user with assigned roles.
/// </summary>
public sealed class CreateUserCommand : IRequest<int>
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string FullName { get; set; }

    public required string UserName { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }

    public required string ConfirmationPassword { get; set; }

    public required List<string> Roles { get; set; }
}