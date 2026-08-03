namespace Portfolio.Application.Common.Interfaces;

/// <summary>
/// Generates authentication tokens for authenticated users.
/// </summary>
public interface ITokenGenerator
{
    /// <summary>
    /// Generates a JSON Web Token (JWT) for the specified user.
    /// </summary>
    /// <param name="userDetails">The user's identifier, username, and assigned roles.</param>
    /// <returns>The generated JWT.</returns>
    string GenerateJWTToken((string userId, string userName, IList<string> roles) userDetails);
}