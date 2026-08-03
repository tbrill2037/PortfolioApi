using MediatR;

namespace Portfolio.Application.Commands.Role.Delete;

/// <summary>
/// Represents a request to delete an existing application role.
/// </summary>
public sealed class DeleteRoleCommand : IRequest<int>
{
    public required string RoleId { get; set; }
}