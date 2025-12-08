using System.Net.Http.Json;
using System.Text.Json;

namespace MovieDatabase.IntegrationTests.Helpers;

public static class GraphQLHelper
{
    public static async Task<HttpResponseMessage> ExecuteQueryAsync(
        HttpClient client,
        string query,
        object? variables = null)
    {
        try
        {
            var request = new
            {
                query = query,
                variables = variables
            };

            return await client.PostAsJsonAsync("/graphql", request);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException(
                $"Failed to execute GraphQL query. Ensure the API is running and accessible. " +
                $"Base Address: {client.BaseAddress}. Original error: {ex.Message}",
                ex);
        }
    }

    public static async Task<GraphQLResponse<T>?> ExecuteQueryAsync<T>(
        HttpClient client,
        string query,
        object? variables = null)
    {
        var response = await ExecuteQueryAsync(client, query, variables);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).\nContent: {content}");
        }

        return JsonSerializer.Deserialize<GraphQLResponse<T>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public static async Task<HttpResponseMessage> ExecuteMutationAsync(
        HttpClient client,
        string mutation,
        object? variables = null)
    {
        return await ExecuteQueryAsync(client, mutation, variables);
    }

    public static async Task<GraphQLResponse<T>?> ExecuteMutationAsync<T>(
        HttpClient client,
        string mutation,
        object? variables = null)
    {
        return await ExecuteQueryAsync<T>(client, mutation, variables);
    }

    public static string LoadQueryFromFile(string relativePath)
    {
        var baseDir = AppContext.BaseDirectory;
        var fullPath = Path.Combine(baseDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"GraphQL file not found: {fullPath}");
        }

        return File.ReadAllText(fullPath);
    }
}