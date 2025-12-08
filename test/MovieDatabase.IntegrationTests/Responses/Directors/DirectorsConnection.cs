namespace MovieDatabase.IntegrationTests.Responses.Directors;

public record DirectorsConnection(
    List<DirectorQueryDto> Nodes
);