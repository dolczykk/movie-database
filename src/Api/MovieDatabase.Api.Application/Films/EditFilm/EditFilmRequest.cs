using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Films;
using MovieDatabase.Api.Core.Interfaces;

namespace MovieDatabase.Api.Application.Films.EditFilm;

public sealed record EditFilmRequest(
    string Id,
    string Title,
    DateOnly ReleaseDate,
    string? Description,
    EditFilmRequest.EditFilmActorPlaceholder[] Actors,
    EditFilmRequest.EditFilmGenrePlaceholder[] Genres,
    EditFilmRequest.EditFilmDirectorPlaceholder Director,
    EditFilmRequest.EditFilmProducerPlaceholder Producer) : IRequest<FilmDto>, IFrom<EditFilmRequest, EditFilmInput>
{
    public sealed record EditFilmActorPlaceholder(string? Id, string? Name, string? Surname);

    public sealed record EditFilmGenrePlaceholder(string? Id, string? Name);

    public sealed record EditFilmDirectorPlaceholder(string? Id, string? Name, string? Surname);

    public sealed record EditFilmProducerPlaceholder(string? Id, string? Name);

    public string UserId { get; set; } = string.Empty;

    public static EditFilmRequest From(EditFilmInput from)
        => new(
            from.Id,
            from.Title,
            from.ReleaseDate,
            from.Description,
            from.Actors.Select(a => new EditFilmActorPlaceholder(a.Id, a.Name, a.Surname)).ToArray(),
            from.Genres.Select(g => new EditFilmGenrePlaceholder(g.Id, g.Name)).ToArray(),
            new EditFilmDirectorPlaceholder(from.Director.Id, from.Director.Name, from.Director.Surname),
            new EditFilmProducerPlaceholder(from.Producer.Id, from.Producer.Name)
        );
}