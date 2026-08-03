using Microsoft.AspNetCore.Http;
using Portfolio.Application.Common.Interfaces;

namespace Portfolio.Infra.Services;

/// <summary>
/// Provides access to the current request client IP address.
/// </summary>
public sealed class ClientIpService : IClientIpService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientIpService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public string? GetClientIp()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}