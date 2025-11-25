using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;

using Bogus;

using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.FakeDataGenerator;
using MovieDatabase.FakeDataGenerator.Extensions;
using MovieDatabase.FakeDataGenerator.Models;

Option<FileInfo> moviesFileOption = new("--file")
{
    Description = "Path to the CSV file containing movie data", Required = true
};

Option<int> countOption = new("--count")
{
    Description = "If > 0, generate this many films using Bogus by sampling CSV rows as templates",
    DefaultValueFactory = _ => 100,
    Required = false
};

RootCommand rootCommand = new("Movie Database Fake Data Generator") { moviesFileOption, countOption };

ParseResult parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0)
{
    foreach (ParseError error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return -1;
}

string moviesCsvPath = parseResult.GetValue(moviesFileOption)!.FullName;
int requestedCount = parseResult.GetValue(countOption);

if (!File.Exists(moviesCsvPath))
{
    Console.Error.WriteLine($"Movies file not found: {moviesCsvPath}");
    return -2;
}

List<Movie> movies = FilmDataLoader.LoadMoviesFromCsv(moviesCsvPath);

if (movies.Count == 0)
{
    Console.Error.WriteLine("No movies parsed from CSV.");
    return -3;
}

Faker faker = new();

int filmCount = requestedCount > 0 ? requestedCount : movies.Count;

(List<Film> filmsList, List<User> users) = faker.GenerateFilmsWithUsers(movies, filmCount);

string outPath = Path.ChangeExtension(moviesCsvPath, ".films.json");
string usersOutPath = Path.ChangeExtension(moviesCsvPath, ".users.json");

await using FileStream fs = File.Create(outPath);
await JsonSerializer.SerializeAsync(fs, filmsList);

await using FileStream usersStream = File.Create(usersOutPath);
await JsonSerializer.SerializeAsync(usersStream, users);

Console.WriteLine($"Wrote {filmsList.Count} films to {outPath}");
Console.WriteLine($"Wrote {users.Count} users to {usersOutPath}");

return 0;