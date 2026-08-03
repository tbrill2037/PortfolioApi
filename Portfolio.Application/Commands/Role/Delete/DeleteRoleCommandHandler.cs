using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.Role.Delete;

/// <summary>
/// Handles requests to delete existing application roles.
/// </summary>
public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, int>
{
    private readonly IIdentityService _identityService;

    public DeleteRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        // Delete the requested role.
        var deleted = await _identityService.DeleteRoleAsync(request.RoleId);

        return deleted ? 1 : 0;
    }
}