using MediatR;
using Portfolio.Application.Common.Exceptions;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Commands.Auth;

/// <summary>
/// Handles authentication requests by validating user credentials,
/// retrieving user information, and issuing a JWT.
/// </summary>
public sealed class AuthCommandHandler : IRequestHandler<AuthCommand, AuthResponseDTO>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenGenerator _tokenGenerator;

    public AuthCommandHandler(
        IIdentityService identityService,
        ITokenGenerator tokenGenerator)
    {
        _identityService = identityService;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponseDTO> Handle(
        AuthCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the user's credentials.
        var isAuthenticated = await _identityService.SigninUserAsync(
            request.UserName,
            request.Password);

        if (!isAuthenticated)
        {
            throw new BadRequestException("Invalid username or password");
        }

        // Retrieve the authenticated user's information.
        var userId = await _identityService.GetUserIdAsync(request.UserName);

        var (_, fullName, userName, email, roles) =
            await _identityService.GetUserDetailsAsync(userId);

        // Generate the user's JWT.
        var token = _tokenGenerator.GenerateJWTToken((userId, userName, roles));

        return new AuthResponseDTO
        {
            UserId = userId,
            Name = userName,
            Token = token
        };
    }
}