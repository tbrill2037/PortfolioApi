using MediatR;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Queries.Role;

/// <summary>
/// Represents a request to retrieve a role by its identifier.
/// </summary>
public sealed class GetRoleByIdQuery : IRequest<RoleResponseDTO>
{
    public required string RoleId { get; set; }
}

/// <summary>
/// Handles requests to retrieve a role by its identifier.
/// </summary>
public sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, RoleResponseDTO>
{
    private readonly IIdentityService _identityService;

    public GetRoleByIdQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RoleResponseDTO> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _identityService.GetRoleByIdAsync(request.RoleId);

        return new RoleResponseDTO
        {
            Id = role.id,
            RoleName = role.roleName
        };
    }
}