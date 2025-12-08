namespace MovieDatabase.IntegrationTests.Responses.Films;

public record PageInfo(
    bool HasNextPage,
    bool HasPreviousPage,
    string? StartCursor,
    string? EndCursor
);