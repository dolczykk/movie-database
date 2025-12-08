namespace MovieDatabase.Api.Application.Films.CreateFilm;

public sealed record CreateFilmInput(
    string Title,
    DateOnly ReleaseDate,
    string? Description,
    CreateFilmInput.ActorPlaceholder[] Actors,
    CreateFilmInput.GenrePlaceholder[] Genres,
    CreateFilmInput.DirectorPlaceholder Director,
    CreateFilmInput.ProducerPlaceholder Producer)
{
    public sealed record ActorPlaceholder(string? Id, string? Name, string? Surname);

    public sealed record GenrePlaceholder(string? Id, string? Name);

    public sealed record DirectorPlaceholder(string? Id, string? Name, string? Surname);

    public sealed record ProducerPlaceholder(string? Id, string? Name);
}