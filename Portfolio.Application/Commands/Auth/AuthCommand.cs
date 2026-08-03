using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Commands.Auth;

/// <summary>
/// Represents a request to authenticate a user and obtain a JWT.
/// </summary>
public sealed class AuthCommand : IRequest<AuthResponseDTO>
{
    public required string UserName { get; set; }

    public required string Password { get; set; }
}