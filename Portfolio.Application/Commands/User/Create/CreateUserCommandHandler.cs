using MediatR;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Application.Commands.User.Create;

/// <summary>
/// Handles requests to create new application users and assign their roles.
/// </summary>
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, int>
{
    private readonly IIdentityService _identityService;

    public CreateUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var created = await _identityService.CreateUserAsync(
            request.UserName,
            request.Password,
            request.Email,
            request.FirstName,
            request.LastName,
            request.FullName,
            request.Roles);

        return created.isSucceed ? 1 : 0;
    }
}