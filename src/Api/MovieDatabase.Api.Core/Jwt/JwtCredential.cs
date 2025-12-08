namespace MovieDatabase.Api.Core.Jwt;

public sealed record JwtCredential(
    JwtCredential.JwtToken AccessToken,
    JwtCredential.JwtToken RefreshToken
)
{
    public sealed record JwtToken(
        string Token,
        DateTime ExpireDate
    );
}