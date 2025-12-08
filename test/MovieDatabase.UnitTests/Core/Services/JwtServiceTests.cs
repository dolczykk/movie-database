using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.Extensions.Options;

using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Jwt;
using MovieDatabase.Api.Core.Services;

using Shouldly;

namespace MovieDatabase.UnitTests.Core.Services;

public class JwtServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = "ThisIsAVerySecureKeyThatIsAtLeast32CharactersLong!",
            AccessTokenExpirationMinutes = 60
        };

        var options = Options.Create(_jwtSettings);
        _jwtService = new JwtService(options);
    }

    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            Email = "test@example.com",
            Role = UserRoles.User
        };

        // Act
        var credentials = _jwtService.GenerateJwtToken(user);

        // Assert
        credentials.AccessToken.ShouldNotBeNull();
        credentials.AccessToken.Token.ShouldNotBeEmpty();
        credentials.AccessToken.ExpireDate.ShouldBeGreaterThan(DateTime.UtcNow);

        credentials.RefreshToken.ShouldNotBeNull();
        credentials.RefreshToken.Token.ShouldNotBeEmpty();
        credentials.RefreshToken.ExpireDate.ShouldBeGreaterThan(DateTime.UtcNow);

        credentials.AccessToken.Token.ShouldNotBe(credentials.RefreshToken.Token);
        credentials.AccessToken.ExpireDate.ShouldNotBeSameAs(credentials.RefreshToken.ExpireDate);
    }

    [Fact]
    public void GenerateJwtToken_ShouldIncludeUserClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "TestUser",
            Email = "test@example.com",
            Role = UserRoles.User
        };

        // Act
        var token = _jwtService.GenerateJwtToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtAccessToken = handler.ReadJwtToken(token.AccessToken.Token);
        var jwtRefreshToken = handler.ReadJwtToken(token.RefreshToken.Token);

        jwtAccessToken.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Jti && c.Value == userId.ToString());
        jwtAccessToken.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "TestUser");
        jwtAccessToken.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        jwtAccessToken.Claims.ShouldContain(c => c.Type == JwtExtendedClaimTypes.Kid);
        jwtAccessToken.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "User");

        jwtRefreshToken.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Jti && c.Value == userId.ToString());
        jwtRefreshToken.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "TestUser");
        jwtRefreshToken.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        jwtRefreshToken.Claims.ShouldContain(c => c.Type == JwtExtendedClaimTypes.Kid);
        jwtRefreshToken.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Fact]
    public void GenerateJwtToken_AccessTokenShouldSetCorrectExpirationTime()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            Email = "test@example.com",
            Role = UserRoles.User
        };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var credential = _jwtService.GenerateJwtToken(user);

        // Assert
        var afterGeneration = DateTime.UtcNow;
        var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        credential.AccessToken.ExpireDate.ShouldBe(expectedExpiration, TimeSpan.FromSeconds(5));
        credential.AccessToken.ExpireDate.ShouldBeGreaterThan(afterGeneration);
    }

    [Fact]
    public void GenerateJwtToken_RefreshTokenShouldSetCorrectExpirationTime()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            Email = "test@example.com",
            Role = UserRoles.User
        };
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var credential = _jwtService.GenerateJwtToken(user);

        // Assert
        var afterGeneration = DateTime.UtcNow;
        var expectedExpiration = beforeGeneration.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        credential.RefreshToken.ExpireDate.ShouldBe(expectedExpiration, TimeSpan.FromSeconds(5));
        credential.RefreshToken.ExpireDate.ShouldBeGreaterThan(afterGeneration);
    }

    [Fact]
    public void GenerateJwtToken_AccessTokenShouldBeDecodable()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            Email = "test@example.com",
            Role = UserRoles.User
        };

        // Act
        var credentials = _jwtService.GenerateJwtToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();

        var canReadAccessToken = handler.CanReadToken(credentials.AccessToken.Token);
        canReadAccessToken.ShouldBeTrue();

        var accessJwtToken = handler.ReadJwtToken(credentials.AccessToken.Token);

        accessJwtToken.ShouldNotBeNull();
        accessJwtToken.Issuer.ShouldBe(_jwtSettings.Issuer);
        accessJwtToken.Audiences.ShouldContain(_jwtSettings.Audience);

        var canReadRefreshToken = handler.CanReadToken(credentials.RefreshToken.Token);
        canReadRefreshToken.ShouldBeTrue();

        var refreshJwtToken = handler.ReadJwtToken(credentials.RefreshToken.Token);

        refreshJwtToken.ShouldNotBeNull();
        refreshJwtToken.Issuer.ShouldBe(_jwtSettings.Issuer);
        refreshJwtToken.Audiences.ShouldContain(_jwtSettings.Audience);
    }

    [Theory]
    [InlineData(UserRoles.User, nameof(UserRoles.User))]
    [InlineData(UserRoles.Moderator, nameof(UserRoles.Moderator))]
    [InlineData(UserRoles.Administrator, nameof(UserRoles.Administrator))]
    public void GenerateJwtToken_ShouldIncludeRoleClaim(UserRoles role, string expectedRoleName)
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            Email = "test@example.com",
            Role = role
        };

        // Act
        var credentials = _jwtService.GenerateJwtToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(credentials.AccessToken.Token);

        // Assert
        jwtToken.Claims.ShouldContain(c =>
            c.Type == ClaimTypes.Role &&
            c.Value == expectedRoleName,
            $"token should contain role claim for {expectedRoleName}");
    }

    [Fact]
    public void GenerateJwtToken_ShouldGenerateDifferentTokensForSameUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            Email = "test@example.com",
            Role = UserRoles.User
        };

        // Act
        var credentials1 = _jwtService.GenerateJwtToken(user);
        var credentials2 = _jwtService.GenerateJwtToken(user);

        // Assert
        credentials1.ShouldNotBe(credentials2, "Each token generation should produce a unique token");
    }

    [Fact]
    public void GenerateJwtToken_WithDifferentUsers_ShouldGenerateDifferentTokens()
    {
        // Arrange
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User1",
            Email = "user1@example.com",
            Role = UserRoles.User
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "User2",
            Email = "user2@example.com",
            Role = UserRoles.Administrator
        };

        // Act
        var credentials1 = _jwtService.GenerateJwtToken(user1);
        var credentials2 = _jwtService.GenerateJwtToken(user2);

        // Assert
        credentials1.ShouldNotBe(credentials2);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken1 = handler.ReadJwtToken(credentials1.AccessToken.Token);
        var jwtToken2 = handler.ReadJwtToken(credentials2.AccessToken.Token);

        var jwtAccessToken1 = handler.ReadJwtToken(credentials1.RefreshToken.Token);
        var jwtAccessToken2 = handler.ReadJwtToken(credentials2.RefreshToken.Token);

        jwtToken1.Subject.ShouldBe("User1");
        jwtToken2.Subject.ShouldBe("User2");

        jwtAccessToken1.Subject.ShouldBe("User1");
        jwtAccessToken2.Subject.ShouldBe("User2");
    }
}