using MovieDatabase.Api.Core.Interfaces;
using MovieDatabase.Api.Core.Jwt;

namespace MovieDatabase.Api.Core.Dtos.Users;

public record RevokeTokenDto(
    RevokeTokenDto.RevokeTokenDto_JwtToken AccessToken,
    RevokeTokenDto.RevokeTokenDto_JwtToken RefreshToken
) : IFrom<RevokeTokenDto, JwtCredential>
{
    public static RevokeTokenDto From(JwtCredential from)
        => new(
            new RevokeTokenDto_JwtToken(from.AccessToken.Token, from.AccessToken.ExpireDate),
            new RevokeTokenDto_JwtToken(from.RefreshToken.Token, from.RefreshToken.ExpireDate)
        );

    public sealed record RevokeTokenDto_JwtToken(string Value, DateTime ExpiresAt);
}