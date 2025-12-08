using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Users;

namespace MovieDatabase.Api.Application.Users.RevokeToken;

public sealed record RevokeTokenRequest(string AccessToken, string RefreshToken) : IRequest<RevokeTokenDto>;