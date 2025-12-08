using MovieDatabase.Api.Core.Dtos.Films;

namespace MovieDatabase.IntegrationTests.Responses.Films;

public record EditFilmResponse(
    FilmDto EditFilm
);