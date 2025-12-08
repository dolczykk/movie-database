namespace MovieDatabase.IntegrationTests.Responses.Actors;

public record ActorQueryDto(
    string? Id,
    string? Name,
    string? Surname
);