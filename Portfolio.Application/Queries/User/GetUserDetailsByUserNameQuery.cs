using MediatR;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Queries.User;

/// <summary>
/// Represents a request to retrieve detailed user information by username.
/// </summary>
public sealed class GetUserDetailsByUserNameQuery : IRequest<UserDetailsResponseDTO>
{
    public required string UserName { get; set; }
}

/// <summary>
/// Handles requests to retrieve detailed user information by username.
/// </summary>
public sealed class GetUserDetailsByUserNameQueryHandler : IRequestHandler<GetUserDetailsByUserNameQuery, UserDetailsResponseDTO>
{
    private readonly IIdentityService _identityService;

    public GetUserDetailsByUserNameQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserDetailsResponseDTO> Handle(
        GetUserDetailsByUserNameQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, fullName, userName, email, roles) =
            await _identityService.GetUserDetailsByUserNameAsync(request.UserName);

        return new UserDetailsResponseDTO
        {
            Id = userId,
            FullName = fullName,
            UserName = userName,
            Email = email,
            Roles = roles
        };
    }
}