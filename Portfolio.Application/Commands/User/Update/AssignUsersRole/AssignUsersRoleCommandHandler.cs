using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.User.Update.AssignUsersRole;

/// <summary>
/// Handles requests to assign application roles to users.
/// </summary>
public sealed class AssignUsersRoleCommandHandler : IRequestHandler<AssignUsersRoleCommand, int>
{
    private readonly IIdentityService _identityService;

    public AssignUsersRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(AssignUsersRoleCommand request, CancellationToken cancellationToken)
    {
        // Assign the requested roles to the user.
        var assigned = await _identityService.AssignUserToRoleAsync(
            request.UserName,
            request.Roles);

        return assigned ? 1 : 0;
    }
}