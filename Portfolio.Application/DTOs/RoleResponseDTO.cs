namespace Portfolio.Application.DTOs;

/// <summary>
/// Represents role information returned by the application.
/// </summary>
public sealed class RoleResponseDTO
{
    public required string Id { get; set; }

    public required string RoleName { get; set; }
}