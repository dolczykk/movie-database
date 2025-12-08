using MovieDatabase.Api.Application.Users.AuthenticateUser;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Exceptions.Users;
using MovieDatabase.Api.Core.Jwt;
using MovieDatabase.Api.Core.Services;
using MovieDatabase.Api.Core.Utils;
using MovieDatabase.Api.Infrastructure.Db.Repositories;
using MovieDatabase.UnitTests.Helpers;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Users;

public class AuthenticateUserRequestHandlerTests
{
    private const int ExpireDateToleranceSeconds = 10;

    private readonly IUserRepository _mockUserRepository;
    private readonly IJwtService _mockJwtService;
    private readonly AuthenticateUserRequestHandler _handler;

    public AuthenticateUserRequestHandlerTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockJwtService = Substitute.For<IJwtService>();
        _handler = new AuthenticateUserRequestHandler(_mockUserRepository, _mockJwtService);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldReturnUserCredentials()
    {
        // Arrange
        const string password = "TestPassword123!";
        var passwordHash = PasswordUtils.HashPassword(password);
        var user = TestDataBuilder.CreateValidUser(
            email: "test@example.com",
            passwordHash: passwordHash
        );

        var request = new AuthenticateUserRequest(
            user.Email,
            password
        );

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("jwt-access-token-12345", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("jwt-refresh-token-12345", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(expectedJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe(expectedJwtCredentials.AccessToken.Token);
        result.ExpireTime?.ShouldBe(expectedJwtCredentials.AccessToken.ExpireDate, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));
        result.Email.ShouldBe(user.Email);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmail_ShouldThrowInvalidUserCredentialsException()
    {
        // Arrange
        var request = new AuthenticateUserRequest(
            "nonexistent@example.com",
             "SomePassword123!"
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(null));

        // Act
        var act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<InvalidUserCredentialsApplicationException>(act);

        _mockJwtService.DidNotReceive().GenerateJwtToken(Arg.Any<User>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ShouldThrowInvalidUserCredentialsException()
    {
        // Arrange
        const string correctPassword = "CorrectPassword123!";
        const string incorrectPassword = "WrongPassword456!";
        var passwordHash = PasswordUtils.HashPassword(correctPassword);

        var user = TestDataBuilder.CreateValidUser(
            email: "test@example.com",
            passwordHash: passwordHash
        );

        var request = new AuthenticateUserRequest(
            Email: user.Email,
            Password: incorrectPassword
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));

        // Act
        var act = async () => await _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<InvalidUserCredentialsApplicationException>(act);

        _mockJwtService.DidNotReceive().GenerateJwtToken(Arg.Any<User>());
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateJwtToken()
    {
        // Arrange
        const string password = "TestPassword123!";
        var passwordHash = PasswordUtils.HashPassword(password);
        var user = TestDataBuilder.CreateValidUser(passwordHash: passwordHash);

        var request = new AuthenticateUserRequest(
            Email: user.Email,
            Password: password
        );

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(expectedJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockJwtService.Received(1).GenerateJwtToken(Arg.Is<User>(u =>
            u.Id == user.Id &&
            u.Email == user.Email &&
            u.Name == user.Name));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTokenWithExpirationTime()
    {
        // Arrange
        const string password = "TestPassword123!";
        var passwordHash = PasswordUtils.HashPassword(password);
        var user = TestDataBuilder.CreateValidUser(passwordHash: passwordHash);

        var request = new AuthenticateUserRequest(
            Email: user.Email,
            Password: password
        );

        var expectedTokenModel = new JwtCredential(
            new JwtCredential.JwtToken("jwt-access-token-abc123", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("jwt-refresh-token-abc123", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(expectedTokenModel);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Token.ShouldBe(expectedTokenModel.AccessToken.Token);
        result.ExpireTime?.ShouldBe(expectedTokenModel.AccessToken.ExpireDate, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));
    }

    [Theory]
    [InlineData(UserRoles.User)]
    [InlineData(UserRoles.Moderator)]
    [InlineData(UserRoles.Administrator)]
    public async Task HandleAsync_WithDifferentUserRoles_ShouldReturnCorrectRole(UserRoles role)
    {
        // Arrange
        var password = "TestPassword123!";
        var passwordHash = PasswordUtils.HashPassword(password);
        var user = TestDataBuilder.CreateValidUser(
            passwordHash: passwordHash,
            role: role
        );

        var request = new AuthenticateUserRequest(
            Email: user.Email,
            Password: password
        );

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(expectedJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Role.ShouldBe(Enum.GetName(role));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyPassword_ShouldThrowInvalidUserCredentialsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateValidUser();
        var request = new AuthenticateUserRequest(
            Email: user.Email,
            Password: string.Empty
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));

        // Act & Assert
        await Should.ThrowAsync<InvalidUserCredentialsApplicationException>(
            () => _handler.HandleAsync(request)
        );
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryGetByEmailOnce()
    {
        // Arrange
        const string password = "TestPassword123!";
        var passwordHash = PasswordUtils.HashPassword(password);
        var user = TestDataBuilder.CreateValidUser(passwordHash: passwordHash);

        var request = new AuthenticateUserRequest(
            Email: user.Email,
            Password: password
        );

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(user));
        _mockJwtService.GenerateJwtToken(user)
            .Returns(expectedJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        await _mockUserRepository.Received(1).GetByEmail(request.Email);
    }
}