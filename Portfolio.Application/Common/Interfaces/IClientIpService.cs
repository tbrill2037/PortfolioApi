namespace Portfolio.Application.Common.Interfaces;

/// <summary>
/// Provides access to the client's IP address for the current request.
/// </summary>
public interface IClientIpService
{
    /// <summary>
    /// Gets the client's IP address.
    /// </summary>
    /// <returns>
    /// The client's IP address if available; otherwise, <see langword="null"/>.
    /// </returns>
    string? GetClientIp();
}