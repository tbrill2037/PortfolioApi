using MediatR;

namespace Portfolio.Application.Commands.Role.Update;

/// <summary>
/// Represents a request to update an existing application role.
/// </summary>
public sealed class UpdateRoleCommand : IRequest<int>
{
    public required string Id { get; set; }

    public required string RoleName { get; set; }
}