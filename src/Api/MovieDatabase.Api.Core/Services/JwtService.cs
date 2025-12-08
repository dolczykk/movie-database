using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Jwt;

namespace MovieDatabase.Api.Core.Services;

public class JwtService(IOptions<JwtSettings> options) : IJwtService
{
    private readonly JwtSettings _settings = options.Value;

    public ClaimsPrincipal ReadPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.Key);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = ClaimTypes.Role
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }

    public JwtCredential GenerateJwtToken(User user)
    {
        var now = DateTime.UtcNow;
        var accessTokenExpires = now.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Name),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtExtendedClaimTypes.Kid, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: accessTokenExpires,
            signingCredentials: creds
        );

        var generatedAccessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshTokenExpires = now.AddDays(_settings.RefreshTokenExpirationDays);

        var refreshToken = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: refreshTokenExpires,
            signingCredentials: creds
        );

        var generatedRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);

        return new JwtCredential(
            new JwtCredential.JwtToken(generatedAccessToken, accessTokenExpires),
            new JwtCredential.JwtToken(generatedRefreshToken, refreshTokenExpires)
        );
    }
}