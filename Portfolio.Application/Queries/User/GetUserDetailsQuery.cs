using MediatR;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Queries.User;

/// <summary>
/// Represents a request to retrieve detailed user information by identifier.
/// </summary>
public sealed class GetUserDetailsQuery : IRequest<UserDetailsResponseDTO>
{
    public required string UserId { get; set; }
}

/// <summary>
/// Handles requests to retrieve detailed user information by identifier.
/// </summary>
public sealed class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, UserDetailsResponseDTO>
{
    private readonly IIdentityService _identityService;

    public GetUserDetailsQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserDetailsResponseDTO> Handle(
        GetUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, fullName, userName, email, roles) =
            await _identityService.GetUserDetailsAsync(request.UserId);

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