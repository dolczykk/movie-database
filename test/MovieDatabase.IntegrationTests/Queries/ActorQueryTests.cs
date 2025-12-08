using MovieDatabase.IntegrationTests.Fixtures;
using MovieDatabase.IntegrationTests.Helpers;
using MovieDatabase.IntegrationTests.Responses.Actors;

using Shouldly;

namespace MovieDatabase.IntegrationTests.Queries;

[Collection("AspireAppHost")]
public class ActorQueryTests(AspireAppHostFixture fixture)
{
    private readonly HttpClient _httpClient = fixture.CreateHttpClient("movies-db-api");

    [Fact]
    public async Task GetActors_WithoutFilter_ShouldReturnAllActors()
    {
        // Arrange
        var query = GraphQLHelper.LoadQueryFromFile("Graphql/Queries/GetActors.graphql");

        // Act
        var response = await GraphQLHelper.ExecuteQueryAsync<ActorsResponse>(_httpClient, query);

        // Assert
        response.ShouldNotBeNull();
        response.Errors.ShouldBeNull();
        response.Data.ShouldNotBeNull();
        response.Data.Actors.ShouldNotBeNull();
        response.Data.Actors.Nodes.ShouldNotBeNull();
        response.Data.Actors.Nodes.ShouldNotBeEmpty();
    }
}