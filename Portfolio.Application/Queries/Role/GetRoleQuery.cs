using MediatR;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Queries.Role;

/// <summary>
/// Represents a request to retrieve all roles.
/// </summary>
public sealed class GetRoleQuery : IRequest<IList<RoleResponseDTO>>
{
}

/// <summary>
/// Handles requests to retrieve all roles.
/// </summary>
public sealed class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, IList<RoleResponseDTO>>
{
    private readonly IIdentityService _identityService;

    public GetRoleQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<IList<RoleResponseDTO>> Handle(
        GetRoleQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _identityService.GetRolesAsync();

        return roles
            .Select(role => new RoleResponseDTO
            {
                Id = role.id,
                RoleName = role.roleName
            })
            .ToList();
    }
}