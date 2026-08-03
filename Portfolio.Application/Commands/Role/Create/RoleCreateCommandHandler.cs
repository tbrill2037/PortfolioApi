using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.Role.Create;

/// <summary>
/// Handles requests to create new application roles.
/// </summary>
public sealed class RoleCreateCommandHandler : IRequestHandler<RoleCreateCommand, int>
{
    private readonly IIdentityService _identityService;

    public RoleCreateCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(RoleCreateCommand request, CancellationToken cancellationToken)
    {
        // Create the requested role.
        var created = await _identityService.CreateRoleAsync(request.RoleName);

        return created ? 1 : 0;
    }
}