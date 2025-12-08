namespace MovieDatabase.IntegrationTests.Responses.Directors;

public record DirectorQueryDto(
    string? Id,
    string? Name,
    string? Surname
);