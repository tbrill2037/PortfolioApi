namespace Portfolio.Application.DTOs;

/// <summary>
/// Represents detailed user information returned by the application.
/// </summary>
public sealed class UserDetailsResponseDTO
{
    public required string Id { get; set; }

    public required string FullName { get; set; }

    public required string UserName { get; set; }

    public required string Email { get; set; }

    public required IList<string> Roles { get; set; }
}