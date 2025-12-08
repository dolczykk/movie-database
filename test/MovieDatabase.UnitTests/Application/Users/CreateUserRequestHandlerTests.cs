using MovieDatabase.Api.Application.Users.CreateUser;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Exceptions.Users;
using MovieDatabase.Api.Core.Jwt;
using MovieDatabase.Api.Core.Services;
using MovieDatabase.Api.Infrastructure.Db.Repositories;
using MovieDatabase.UnitTests.Helpers;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Users;

public class CreateUserRequestHandlerTests
{
    private const int ExpireDateToleranceSeconds = 10;

    private readonly IUserRepository _mockUserRepository;
    private readonly IJwtService _mockJwtService;
    private readonly CreateUserRequestHandler _handler;

    public CreateUserRequestHandlerTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockJwtService = Substitute.For<IJwtService>();
        _handler = new CreateUserRequestHandler(_mockUserRepository, _mockJwtService);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest();

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("test-access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("test-refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        _mockUserRepository.Received(1).Add(Arg.Is<User>(u =>
            u.Name == request.Username &&
            u.Email == request.Email));
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldReturnUserCredentialsWithToken()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest();

        var expectedJwtModel = new JwtCredential(
            new JwtCredential.JwtToken("jwt-access-token-12345", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("jwt-refresh-token-12345", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtModel);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe(expectedJwtModel.AccessToken.Token);
        result.ExpireTime?.ShouldBe(expectedJwtModel.AccessToken.ExpireDate, TimeSpan.FromSeconds(ExpireDateToleranceSeconds));
        result.Username.ShouldBe(request.Username);
        result.Email.ShouldBe(request.Email);
        result.Role.ShouldBe(nameof(UserRoles.User));
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ShouldThrowDuplicateEmailException()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest(email: "existing@example.com");
        var existingUser = TestDataBuilder.CreateValidUser(email: "existing@example.com");

        _mockUserRepository.GetByEmail(request.Email)
            .Returns(Task.FromResult<User?>(existingUser));

        // Act
        Func<Task> act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<DuplicateEmailApplicationException>(act);

        _mockUserRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task HandleAsync_ShouldHashPassword()
    {
        // Arrange
        const string plainPassword = "PlainPassword123!";
        var request = TestDataBuilder.CreateValidCreateUserRequest(password: plainPassword);

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockUserRepository.Received(1).Add(Arg.Is<User>(u =>
            u.PasswordHash != plainPassword &&
            u.PasswordHash.StartsWith("$2")));
    }

    [Fact]
    public async Task HandleAsync_ShouldSetUserRoleToUser()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest();

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.Role.ShouldBe(nameof(UserRoles.User));
        _mockUserRepository.Received(1).Add(Arg.Is<User>(u =>
            u.Role == UserRoles.User));
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryAddOnce()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest();

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockUserRepository.Received(1).Add(Arg.Any<User>());
        await _mockUserRepository.Received(1).GetByEmail(request.Email);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallJwtServiceToGenerateToken()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest();

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtCredentials);

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockJwtService.Received(1).GenerateJwtToken(Arg.Is<User>(u =>
            u.Name == request.Username &&
            u.Email == request.Email &&
            u.Role == UserRoles.User));
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user+tag@domain.co.uk")]
    [InlineData("admin@test-domain.com")]
    public async Task HandleAsync_WithVariousEmailFormats_ShouldSucceed(string email)
    {
        // Arrange
        var request = TestDataBuilder.CreateValidCreateUserRequest(email: email);

        var expectedJwtCredentials = new JwtCredential(
            new JwtCredential.JwtToken("access-token", DateTime.UtcNow.AddHours(1)),
            new JwtCredential.JwtToken("refresh-token", DateTime.UtcNow.AddDays(7))
        );

        _mockUserRepository.GetByEmail(Arg.Any<string>())
            .Returns(Task.FromResult<User?>(null));
        _mockJwtService.GenerateJwtToken(Arg.Any<User>())
            .Returns(expectedJwtCredentials);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe(email);
    }
}