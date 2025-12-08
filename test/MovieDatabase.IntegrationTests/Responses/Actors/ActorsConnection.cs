namespace MovieDatabase.IntegrationTests.Responses.Actors;

public record ActorsConnection(
    List<ActorQueryDto> Nodes
);