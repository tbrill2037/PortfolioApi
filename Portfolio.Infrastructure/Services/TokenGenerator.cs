using Microsoft.IdentityModel.Tokens;
using Portfolio.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Portfolio.Infra.Services;

/// <summary>
/// Generates JWT authentication tokens for authenticated users.
/// </summary>
public class TokenGenerator : ITokenGenerator
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expiryMinutes;

    public TokenGenerator(string key, string issueer, string audience, double expiryMinutes)
    {
        _key = key;
        _issuer = issueer;
        _audience = audience;
        _expiryMinutes = expiryMinutes;
    }

    /// <summary>
    /// Generates a JWT token containing user identity and role claims.
    /// </summary>
    public string GenerateJWTToken((string userId, string userName, IList<string> roles) userDetails)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var (userId, userName, roles) = userDetails;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userName),
            new Claim(JwtRegisteredClaimNames.Jti, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim("UserId", userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_expiryMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}