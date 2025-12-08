using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using MovieDatabase.Api.Application.Users.RevokeToken;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Exceptions.Auth;
using MovieDatabase.Api.Core.Jwt;
using MovieDatabase.Api.Core.Services;
using MovieDatabase.Api.Core.Utils;
using MovieDatabase.Api.Infrastructure.Db.Repositories;
using MovieDatabase.UnitTests.Helpers;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Users;

public class RevokeTokenRequestHandlerTests
{
    private const int ExpireDateToleranceSeconds = 10;

    private readonly IUserRepository _mockUserRepository;
    private readonly IJwtService _mockJwtService;
    private readonly RevokeTokenRequestHandler _handler;

    public RevokeTokenRequestHandlerTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockJwtService = Substitute.For<IJwtService>();
        _handler = new RevokeTokenRequestHandler(_mockUserRepository, _mockJwtService);
    }

    [Fact]
    public async Task HandleAsync_WithValidTokens_ShouldRevokeOldTokenAndCreateNew()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const string oldAccessToken = "old-access-token";
        const string oldRefreshToken = "old-refresh-token";

        var user = TestDataBuilder.CreateValidUser(id: Guid.Parse(userId));
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash(oldAccessToken),
                RefreshToken = HashUtils.ComputeHash(oldRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest(oldAccessToken, oldRefreshToken);

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        // mock reading principal from expired token to return userId in JTI
        _mockJwtService.ReadPrincipalFromExpiredToken(oldAccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(oldAccessToken), HashUtils.ComputeHash(oldRefreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.Value.ShouldBe(newJwtCredentials.AccessToken.Token);
        result.AccessToken.ExpiresAt.ShouldBe(newJwtCredentials.AccessToken.ExpireDate, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));
        result.RefreshToken.Value.ShouldBe(newJwtCredentials.RefreshToken.Token);
        result.RefreshToken.ExpiresAt.ShouldBe(newJwtCredentials.RefreshToken.ExpireDate, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));

        user.Tokens.First(t => t.AccessToken == HashUtils.ComputeHash(oldAccessToken)).IsRevoked.ShouldBeTrue();

        user.Tokens.ShouldContain(t =>
            t.AccessToken == HashUtils.ComputeHash(newJwtCredentials.AccessToken.Token) &&
            t.RefreshToken == HashUtils.ComputeHash(newJwtCredentials.RefreshToken.Token) &&
            !t.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTokens_ShouldThrowTokenCannotBeRevokedApplicationException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = new RevokeTokenRequest("invalid-access-token", "invalid-refresh-token");

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(request.AccessToken), HashUtils.ComputeHash(request.RefreshToken))
            .Returns(Task.FromResult<User?>(null));

        // Act
        var act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<TokenCannotBeRevokedApplicationException>(act);

        _mockJwtService.DidNotReceive().GenerateJwtToken(Arg.Any<User>());
    }

    [Fact]
    public async Task HandleAsync_WithNullUser_ShouldThrowTokenCannotBeRevokedApplicationException()
    {
        // Arrange
        var request = new RevokeTokenRequest("access-token", "refresh-token");

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) })));

        _mockUserRepository.FindUserToRevokeToken(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));

        // Act
        var act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<TokenCannotBeRevokedApplicationException>(act);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallFindUserToRevokeTokenWithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const string accessToken = "test-access-token";
        const string refreshToken = "test-refresh-token";

        var user = TestDataBuilder.CreateValidUser();
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash(accessToken),
                RefreshToken = HashUtils.ComputeHash(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest(accessToken, refreshToken);

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockJwtService.ReadPrincipalFromExpiredToken(accessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(accessToken), HashUtils.ComputeHash(refreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        await _mockUserRepository.Received(1).FindUserToRevokeToken(userId, HashUtils.ComputeHash(accessToken), HashUtils.ComputeHash(refreshToken));
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateNewJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = TestDataBuilder.CreateValidUser(id: Guid.Parse(userId));
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash("old-access-token"),
                RefreshToken = HashUtils.ComputeHash("old-refresh-token"),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest("old-access-token", "old-refresh-token");

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(request.AccessToken), HashUtils.ComputeHash(request.RefreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockJwtService.Received(1).GenerateJwtToken(Arg.Is<User>(u =>
            u.Id == user.Id &&
            u.Email == user.Email));
    }

    [Fact]
    public async Task HandleAsync_ShouldAddNewTokenToUserTokensList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = TestDataBuilder.CreateValidUser(id: Guid.Parse(userId));
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash("old-access-token"),
                RefreshToken = HashUtils.ComputeHash("old-refresh-token"),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest("old-access-token", "old-refresh-token");

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(request.AccessToken), HashUtils.ComputeHash(request.RefreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        var initialTokenCount = user.Tokens.Count;

        // Act
        await _handler.HandleAsync(request);

        // Assert
        user.Tokens.Count.ShouldBe(initialTokenCount + 1);
        user.Tokens.ShouldContain(t =>
            t.AccessToken == HashUtils.ComputeHash(newJwtCredentials.AccessToken.Token) &&
            t.RefreshToken == HashUtils.ComputeHash(newJwtCredentials.RefreshToken.Token) &&
            t.ExpiresAt == newJwtCredentials.RefreshToken.ExpireDate &&
            !t.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkOldTokenAsRevoked()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const string oldAccessToken = "old-access-token";
        const string oldRefreshToken = "old-refresh-token";

        var user = TestDataBuilder.CreateValidUser(id: Guid.Parse(userId));
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash(oldAccessToken),
                RefreshToken = HashUtils.ComputeHash(oldRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest(oldAccessToken, oldRefreshToken);

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(oldAccessToken), HashUtils.ComputeHash(oldRefreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        var oldToken = user.Tokens.First(t => t.AccessToken == HashUtils.ComputeHash(oldAccessToken));
        oldToken.IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithMultipleTokens_ShouldOnlyRevokeMatchingToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        const string targetAccessToken = "target-access-token";
        const string targetRefreshToken = "target-refresh-token";

        var user = TestDataBuilder.CreateValidUser(id: Guid.Parse(userId));
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash("other-access-token-1"),
                RefreshToken = HashUtils.ComputeHash("other-refresh-token-1"),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            },

            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash(targetAccessToken),
                RefreshToken = HashUtils.ComputeHash(targetRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            },

            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash("other-access-token-2"),
                RefreshToken = HashUtils.ComputeHash("other-refresh-token-2"),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest(targetAccessToken, targetRefreshToken);

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("new-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(targetAccessToken), HashUtils.ComputeHash(targetRefreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        user.Tokens.Count(t => t.IsRevoked).ShouldBe(1);
        user.Tokens.First(t => t.AccessToken == HashUtils.ComputeHash(targetAccessToken)).IsRevoked.ShouldBeTrue();
        user.Tokens.First(t => t.AccessToken == HashUtils.ComputeHash("other-access-token-1")).IsRevoked.ShouldBeFalse();
        user.Tokens.First(t => t.AccessToken == HashUtils.ComputeHash("other-access-token-2")).IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRevokeTokenDtoWithCorrectValues()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = TestDataBuilder.CreateValidUser(id: Guid.Parse(userId));
        user.Tokens =
        [
            new ClaimToken
            {
                AccessToken = HashUtils.ComputeHash("old-access-token"),
                RefreshToken = HashUtils.ComputeHash("old-refresh-token"),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            }
        ];

        var request = new RevokeTokenRequest("old-access-token", "old-refresh-token");

        var accessTokenExpiry = DateTime.UtcNow.AddHours(1);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        var newJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("new-access-token-value", accessTokenExpiry),
            new JwtCredential.JwtToken("new-refresh-token-value", refreshTokenExpiry)
        );

        _mockJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Jti, userId) })));

        _mockUserRepository.FindUserToRevokeToken(userId, HashUtils.ComputeHash(request.AccessToken), HashUtils.ComputeHash(request.RefreshToken))
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(newJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNull();
        result.AccessToken.Value.ShouldBe("new-access-token-value");
        result.AccessToken.ExpiresAt.ShouldBe(accessTokenExpiry, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));
        result.RefreshToken.ShouldNotBeNull();
        result.RefreshToken.Value.ShouldBe("new-refresh-token-value");
        result.RefreshToken.ExpiresAt.ShouldBe(refreshTokenExpiry, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));
    }
}