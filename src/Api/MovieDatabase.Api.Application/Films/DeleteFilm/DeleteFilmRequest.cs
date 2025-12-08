using MovieDatabase.Api.Core.Cqrs;

namespace MovieDatabase.Api.Application.Films.DeleteFilm;

public sealed record DeleteFilmRequest(string FilmId, string UserId) : IRequest<string>;