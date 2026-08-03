using MediatR;

namespace Portfolio.Application.Commands.User.Update.AssignUsersRole;

/// <summary>
/// Represents a request to assign one or more roles to an existing user.
/// </summary>
public sealed class AssignUsersRoleCommand : IRequest<int>
{
    public required string UserName { get; set; }

    public required IList<string> Roles { get; set; }
}