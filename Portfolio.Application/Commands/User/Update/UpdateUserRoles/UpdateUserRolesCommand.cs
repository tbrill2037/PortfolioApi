using MediatR;

namespace Portfolio.Application.Commands.User.Update.UpdateUserRoles;

/// <summary>
/// Represents a request to update the roles assigned to an existing user.
/// </summary>
public sealed class UpdateUserRolesCommand : IRequest<int>
{
    public required string UserName { get; set; }

    public required IList<string> Roles { get; set; }
}