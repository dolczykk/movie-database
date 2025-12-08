using MovieDatabase.Api.Core.Dtos.Users;

namespace MovieDatabase.IntegrationTests.Responses.Users;

public record LoginUserResponse(
    UserCredentialsDto LoginUser
);