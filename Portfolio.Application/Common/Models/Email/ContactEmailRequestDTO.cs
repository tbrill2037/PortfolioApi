namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the data required to submit a contact email request.
/// </summary>
public sealed class ContactEmailRequestDTO
{
    public required string Email { get; init; }
     
    public required string Name { get; init; }

    public required string Message { get; init; }
}