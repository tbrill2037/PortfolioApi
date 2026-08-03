using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.User.Update.EditUserProfile;

/// <summary>
/// Handles requests to update user profile information and assigned roles.
/// </summary>
public sealed class EditUserProfileCommandHandler : IRequestHandler<EditUserProfileCommand, int>
{
    private readonly IIdentityService _identityService;

    public EditUserProfileCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(EditUserProfileCommand request, CancellationToken cancellationToken)
    {
        // Update the user's profile information and assigned roles.
        var updated = await _identityService.UpdateUserProfileAsync(
            request.Id,
            request.FullName,
            request.Email,
            request.Roles);

        return updated ? 1 : 0;
    }
}