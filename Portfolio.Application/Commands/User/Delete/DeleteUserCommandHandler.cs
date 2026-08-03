using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.User.Delete;

/// <summary>
/// Handles requests to delete existing application users.
/// </summary>
public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, int>
{
    private readonly IIdentityService _identityService;

    public DeleteUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Delete the requested user.
        var deleted = await _identityService.DeleteUserAsync(request.Id);

        return deleted ? 1 : 0;
    }
}