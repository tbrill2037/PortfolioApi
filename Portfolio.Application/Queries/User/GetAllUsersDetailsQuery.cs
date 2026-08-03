using MediatR;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Queries.User;

/// <summary>
/// Represents a request to retrieve detailed information for all users.
/// </summary>
public sealed class GetAllUsersDetailsQuery : IRequest<List<UserDetailsResponseDTO>>
{
}

/// <summary>
/// Handles requests to retrieve detailed information for all users.
/// </summary>
public sealed class GetAllUsersDetailsQueryHandler : IRequestHandler<GetAllUsersDetailsQuery, List<UserDetailsResponseDTO>>
{
    private readonly IIdentityService _identityService;

    public GetAllUsersDetailsQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<List<UserDetailsResponseDTO>> Handle(
        GetAllUsersDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _identityService.GetAllUsersDetailsAsync();

        return users
            .Select(user => new UserDetailsResponseDTO
            {
                Id = user.id,
                FullName = user.userName,
                UserName = user.userName,
                Email = user.email,
                Roles = user.roles
            })
            .ToList();
    }
}