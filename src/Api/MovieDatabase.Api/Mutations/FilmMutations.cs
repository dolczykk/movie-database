using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using HotChocolate.Authorization;

using MovieDatabase.Api.Application.Films.CreateFilm;
using MovieDatabase.Api.Application.Films.DeleteFilm;
using MovieDatabase.Api.Application.Films.EditFilm;
using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Dtos.Films;

namespace MovieDatabase.Api.Mutations;

[ExtendObjectType("Mutation")]
public class FilmMutations
{
    [Authorize(Roles = [nameof(UserRoles.Administrator)])]
    public async Task<FilmDto> CreateFilm(ClaimsPrincipal claimsPrincipal, CreateFilmInput input,
        [Service] IDispatcher dispatcher)
    {
        var userId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti);

        var request = CreateFilmRequest.From(input);
        request.CreatorId = userId?.Value;

        var result = await dispatcher.Dispatch(request);

        return result;
    }

    [Authorize(Roles = [nameof(UserRoles.Administrator)])]
    public async Task<string> DeleteFilm(ClaimsPrincipal claimsPrincipal, string filmId,
        [Service] IDispatcher dispatcher)
    {
        var userId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti);

        var request = new DeleteFilmRequest(filmId, userId!.Value);

        var result = await dispatcher.Dispatch(request);

        return result;
    }

    [Authorize(Roles = [nameof(UserRoles.Administrator), nameof(UserRoles.Moderator)])]
    public async Task<FilmDto> EditFilm(ClaimsPrincipal claimsPrincipal, EditFilmInput input,
        [Service] IDispatcher dispatcher)
    {
        var userId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti);

        var request = EditFilmRequest.From(input);
        request.UserId = userId!.Value;

        var result = await dispatcher.Dispatch(request);

        return result;
    }
}