using MovieDatabase.Api.Core.Dtos.Films;

namespace MovieDatabase.IntegrationTests.Responses.Films;

public record FilmQueryDto(
    string? Id,
    string? Title,
    string? Description,
    string? ReleaseDate,
    List<ActorDto>? Actors,
    DirectorDto? Director,
    List<GenreDto>? Genres,
    ProducerDto? Producer
);