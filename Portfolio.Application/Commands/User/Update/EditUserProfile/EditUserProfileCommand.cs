using MediatR;

namespace Portfolio.Application.Commands.User.Update.EditUserProfile;

/// <summary>
/// Represents a request to update an existing user's profile information and roles.
/// </summary>
public sealed class EditUserProfileCommand : IRequest<int>
{
    public required string Id { get; set; }

    public required string FullName { get; set; }

    public required string Email { get; set; }

    public required List<string> Roles { get; set; }
}