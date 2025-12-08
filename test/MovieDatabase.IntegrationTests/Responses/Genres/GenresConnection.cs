namespace MovieDatabase.IntegrationTests.Responses.Genres;

public record GenresConnection(
    List<GenreQueryDto> Nodes
);