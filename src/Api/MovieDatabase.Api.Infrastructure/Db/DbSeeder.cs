using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Documents.Users;

using Path = System.IO.Path;

namespace MovieDatabase.Api.Infrastructure.Db;

public static class DbSeeder
{
    private const string SeedDataPath = "Data";

    public static async Task SeedUsers(DbContext context, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(Path.Combine(SeedDataPath, "users.json"));
        var users = await JsonSerializer.DeserializeAsync<User[]>(fileStream, cancellationToken: cancellationToken);

        await context.Set<User>().AddRangeAsync(users);
    }

    public static async Task SeedFilms(DbContext context, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(Path.Combine(SeedDataPath, "films.json"));
        var films = await JsonSerializer.DeserializeAsync<Film[]>(fileStream, cancellationToken: cancellationToken);

        await context.Set<Film>().AddRangeAsync(films);
    }
}