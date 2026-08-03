namespace Portfolio.Application.DTOs;

/// <summary>
/// Represents the data returned after successful authentication.
/// </summary>
public sealed class AuthResponseDTO
{
    public required string UserId { get; set; }

    public required string Name { get; set; }

    public required string Token { get; set; }
}