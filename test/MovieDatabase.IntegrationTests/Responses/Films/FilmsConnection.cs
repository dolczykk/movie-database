namespace MovieDatabase.IntegrationTests.Responses.Films;

public record FilmsConnection(
    List<FilmQueryDto> Nodes,
    PageInfo? PageInfo
);