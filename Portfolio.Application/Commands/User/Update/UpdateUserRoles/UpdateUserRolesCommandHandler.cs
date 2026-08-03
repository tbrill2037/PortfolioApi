using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.User.Update.UpdateUserRoles;

/// <summary>
/// Handles requests to update the roles assigned to existing users.
/// </summary>
public sealed class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, int>
{
    private readonly IIdentityService _identityService;

    public UpdateUserRolesCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        // Update the user's assigned roles.
        var updated = await _identityService.UpdateUserRolesAsync(
            request.UserName,
            request.Roles);

        return updated ? 1 : 0;
    }
}