using System.Security.Claims;

using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Jwt;

namespace MovieDatabase.Api.Core.Services;

public interface IJwtService
{
    ClaimsPrincipal ReadPrincipalFromExpiredToken(string token);
    JwtCredential GenerateJwtToken(User user);
}