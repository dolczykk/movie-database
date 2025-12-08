using MovieDatabase.Api.Core.Dtos.Users;

namespace MovieDatabase.IntegrationTests.Responses.Users;

public record RegisterUserResponse(
    UserCredentialsDto RegisterUser
);