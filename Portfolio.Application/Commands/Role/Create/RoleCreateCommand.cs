using MediatR;

namespace Portfolio.Application.Commands.Role.Create;

/// <summary>
/// Represents a request to create a new application role.
/// </summary>
public sealed class RoleCreateCommand : IRequest<int>
{
    public required string RoleName { get; set; }
}