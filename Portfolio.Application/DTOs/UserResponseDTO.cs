namespace Portfolio.Application.DTOs;

/// <summary>
/// Represents user information returned by the application.
/// </summary>
public sealed class UserResponseDTO
{
    public required string Id { get; set; }

    public required string FullName { get; set; }

    public required string UserName { get; set; }

    public required string Email { get; set; }
}