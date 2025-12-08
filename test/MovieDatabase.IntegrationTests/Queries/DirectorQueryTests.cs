using MovieDatabase.IntegrationTests.Fixtures;
using MovieDatabase.IntegrationTests.Helpers;
using MovieDatabase.IntegrationTests.Responses.Directors;

using Shouldly;

namespace MovieDatabase.IntegrationTests.Queries;

[Collection("AspireAppHost")]
public class DirectorQueryTests(AspireAppHostFixture fixture)
{
    private readonly HttpClient _httpClient = fixture.CreateHttpClient("movies-db-api");

    [Fact]
    public async Task GetDirectors_WithoutFilter_ShouldReturnAllDirectors()
    {
        // Arrange
        var query = GraphQLHelper.LoadQueryFromFile("Graphql/Queries/GetDirectors.graphql");

        // Act
        var response = await GraphQLHelper.ExecuteQueryAsync<DirectorsResponse>(_httpClient, query);

        // Assert
        response.ShouldNotBeNull();
        response.Errors.ShouldBeNull();
        response.Data.ShouldNotBeNull();
        response.Data.Directors.ShouldNotBeNull();
        response.Data.Directors.Nodes.ShouldNotBeNull();
        response.Data.Directors.Nodes.ShouldNotBeEmpty();
    }
}