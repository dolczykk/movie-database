using MovieDatabase.Api.Application.Users.AuthenticateUser;
using MovieDatabase.Api.Application.Users.CreateUser;
using MovieDatabase.IntegrationTests.Fixtures;
using MovieDatabase.IntegrationTests.Helpers;
using MovieDatabase.IntegrationTests.Responses.Users;

using Shouldly;

namespace MovieDatabase.IntegrationTests.Mutations;

[Collection("AspireAppHost")]
public class UserMutationTests(AspireAppHostFixture fixture)
{
    private readonly HttpClient _httpClient = fixture.CreateHttpClient("movies-db-api");

    [Fact]
    public async Task RegisterUser_WithValidData_ShouldReturnCredentials()
    {
        // Arrange
        var mutation = GraphQLHelper.LoadQueryFromFile("Graphql/Mutations/RegisterUser.graphql");

        var request = new CreateUserRequest(
            $"testuser_{Guid.NewGuid():N}",
            $"test_{Guid.NewGuid():N}@example.com",
            "SecurePassword123!"
        );

        var variables = new { request };

        // Act
        var response = await GraphQLHelper.ExecuteMutationAsync<RegisterUserResponse>(
            _httpClient, mutation, variables);

        // Assert
        response.ShouldNotBeNull();
        response.Errors.ShouldBeNull();
        response.Data.ShouldNotBeNull();
        response.Data.ShouldNotBeNull();
        response.Data.RegisterUser.Token.ShouldNotBeNull();
        response.Data.RegisterUser.ExpireTime.ShouldNotBeNull();
        (response.Data.RegisterUser.ExpireTime > DateTime.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterUser_WithDuplicateEmail_ShouldReturnError()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@example.com";

        var mutation = GraphQLHelper.LoadQueryFromFile("Graphql/Mutations/RegisterUser.graphql");

        var request1 = new CreateUserRequest(
            "user1",
            email,
            "Password123!"
        );

        // Act
        await GraphQLHelper.ExecuteMutationAsync<RegisterUserResponse>(_httpClient, mutation, new { request = request1 });

        var request2 = new CreateUserRequest(
            "user2",
            email,
            "Password123!"
        );

        var response = await GraphQLHelper.ExecuteMutationAsync(_httpClient, mutation, new { request = request2 });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var hasError = content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                       response.StatusCode == System.Net.HttpStatusCode.BadRequest;
        hasError.ShouldBeTrue("Expected error for duplicate email");
    }

    [Fact]
    public async Task LoginUser_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var email = $"logintest_{Guid.NewGuid():N}@example.com";
        const string password = "SecurePassword123!";

        var registerRequest = new CreateUserRequest(
            $"loginuser_{Guid.NewGuid():N}",
            email,
            password
        );

        // Act
        var registerMutation = GraphQLHelper.LoadQueryFromFile("Graphql/Mutations/RegisterUser.graphql");

        await GraphQLHelper.ExecuteMutationAsync<RegisterUserResponse>(
            _httpClient, registerMutation, new { request = registerRequest });

        var loginMutation = GraphQLHelper.LoadQueryFromFile("Graphql/Mutations/LoginUser.graphql");

        var loginRequest = new AuthenticateUserRequest(
            email,
            password
        );

        var response = await GraphQLHelper.ExecuteMutationAsync<LoginUserResponse>(
            _httpClient, loginMutation, new { request = loginRequest });

        // Assert
        response.ShouldNotBeNull();
        response.Errors.ShouldBeNull();
        response.Data.ShouldNotBeNull();
        response.Data.LoginUser.ShouldNotBeNull();
        response.Data.LoginUser.Token.ShouldNotBeNull();
    }

    [Fact]
    public async Task LoginUser_WithInvalidCredentials_ShouldReturnError()
    {
        // Arrange
        var mutation = GraphQLHelper.LoadQueryFromFile("Graphql/Mutations/LoginUser.graphql");

        var request = new AuthenticateUserRequest(
            "nonexistent@example.com",
            "WrongPassword123!"
        );

        // Act
        var response = await GraphQLHelper.ExecuteMutationAsync(_httpClient, mutation, new { request });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var hasError = content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                       response.StatusCode == System.Net.HttpStatusCode.BadRequest;
        hasError.ShouldBeTrue("Expected error for invalid credentials");
    }
}