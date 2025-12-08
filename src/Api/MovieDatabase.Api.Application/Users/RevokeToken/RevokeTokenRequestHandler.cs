using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Dtos.Users;
using MovieDatabase.Api.Core.Exceptions.Auth;
using MovieDatabase.Api.Core.Services;
using MovieDatabase.Api.Core.Utils;
using MovieDatabase.Api.Infrastructure.Db.Repositories;

namespace MovieDatabase.Api.Application.Users.RevokeToken;

public sealed class RevokeTokenRequestHandler(
    IUserRepository userRepository,
    IJwtService jwtService
) : IRequestHandler<RevokeTokenRequest, RevokeTokenDto>
{
    public async Task<RevokeTokenDto> HandleAsync(RevokeTokenRequest request)
    {
        var decodeJwt = jwtService.ReadPrincipalFromExpiredToken(request.AccessToken);

        var userId = decodeJwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
        if (userId is null)
        {
            throw new TokenCannotBeRevokedApplicationException();
        }

        var hashedRequestAccessToken = HashUtils.ComputeHash(request.AccessToken);
        var hashedRequestRefreshToken = HashUtils.ComputeHash(request.RefreshToken);

        var user = await userRepository.FindUserToRevokeToken(userId.Value, hashedRequestAccessToken, hashedRequestRefreshToken);
        if (user is null)
        {
            throw new TokenCannotBeRevokedApplicationException();
        }

        var credentials = jwtService.GenerateJwtToken(user);

        user.Tokens.Find(x => x.AccessToken == hashedRequestAccessToken && x.RefreshToken == hashedRequestRefreshToken)!.IsRevoked = true;
        user.Tokens.Add(new ClaimToken
        {
            AccessToken = HashUtils.ComputeHash(credentials.AccessToken.Token),
            RefreshToken = HashUtils.ComputeHash(credentials.RefreshToken.Token),
            ExpiresAt = credentials.RefreshToken.ExpireDate,
            IsRevoked = false
        });

        userRepository.Update(user);

        return RevokeTokenDto.From(credentials);
    }
}