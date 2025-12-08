namespace MovieDatabase.Api.Application.Films.EditFilm;

public sealed record EditFilmInput(
    string Id,
    string Title,
    DateOnly ReleaseDate,
    string? Description,
    EditFilmInput.EditFilmActorPlaceholder[] Actors,
    EditFilmInput.EditFilmGenrePlaceholder[] Genres,
    EditFilmInput.EditFilmDirectorPlaceholder Director,
    EditFilmInput.EditFilmProducerPlaceholder Producer)
{
    public sealed record EditFilmActorPlaceholder(string? Id, string? Name, string? Surname);

    public sealed record EditFilmGenrePlaceholder(string? Id, string? Name);

    public sealed record EditFilmDirectorPlaceholder(string? Id, string? Name, string? Surname);

    public sealed record EditFilmProducerPlaceholder(string? Id, string? Name);
}