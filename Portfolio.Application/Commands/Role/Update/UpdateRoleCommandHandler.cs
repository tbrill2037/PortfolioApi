using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.Role.Update;

/// <summary>
/// Handles requests to update existing application roles.
/// </summary>
public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, int>
{
    private readonly IIdentityService _identityService;

    public UpdateRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // Update the requested role.
        var updated = await _identityService.UpdateRoleAsync(request.Id, request.RoleName);

        return updated ? 1 : 0;
    }
}