using MediatR;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Queries.User;

/// <summary>
/// Represents a request to retrieve all users.
/// </summary>
public sealed class GetUserQuery : IRequest<List<UserResponseDTO>>
{
}

/// <summary>
/// Handles requests to retrieve all users.
/// </summary>
public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, List<UserResponseDTO>>
{
    private readonly IIdentityService _identityService;

    public GetUserQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<List<UserResponseDTO>> Handle(
        GetUserQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _identityService.GetAllUsersAsync();

        return users
            .Select(user => new UserResponseDTO
            {
                Id = user.id,
                FullName = user.fullName,
                UserName = user.userName,
                Email = user.email
            })
            .ToList();
    }
}