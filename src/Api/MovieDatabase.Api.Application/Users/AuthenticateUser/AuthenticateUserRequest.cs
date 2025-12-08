using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Users;

namespace MovieDatabase.Api.Application.Users.AuthenticateUser;

public sealed record AuthenticateUserRequest(
    string Email,
    string Password
) : IRequest<UserCredentialsDto>;