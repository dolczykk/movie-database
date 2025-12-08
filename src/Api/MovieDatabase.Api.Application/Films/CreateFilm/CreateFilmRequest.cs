using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Films;
using MovieDatabase.Api.Core.Interfaces;

namespace MovieDatabase.Api.Application.Films.CreateFilm;

public sealed record CreateFilmRequest(
    string Title,
    DateOnly ReleaseDate,
    string? Description,
    CreateFilmRequest.ActorPlaceholder[] Actors,
    CreateFilmRequest.GenrePlaceholder[] Genres,
    CreateFilmRequest.DirectorPlaceholder Director,
    CreateFilmRequest.ProducerPlaceholder Producer) : IRequest<FilmDto>, IFrom<CreateFilmRequest, CreateFilmInput>
{
    public sealed record ActorPlaceholder(string? Id, string Name, string Surname);

    public sealed record GenrePlaceholder(string? Id, string Name);

    public sealed record DirectorPlaceholder(string? Id, string Name, string Surname);

    public sealed record ProducerPlaceholder(string? Id, string Name);

    public string CreatorId { get; set; } = string.Empty;

    public static CreateFilmRequest From(CreateFilmInput from)
        => new(
            from.Title,
            from.ReleaseDate,
            from.Description,
            from.Actors.Select(a => new ActorPlaceholder(a.Id, a.Name, a.Surname)).ToArray(),
            from.Genres.Select(g => new GenrePlaceholder(g.Id, g.Name)).ToArray(),
            new DirectorPlaceholder(from.Director.Id, from.Director.Name, from.Director.Surname),
            new ProducerPlaceholder(from.Producer.Id, from.Producer.Name)
        );
}