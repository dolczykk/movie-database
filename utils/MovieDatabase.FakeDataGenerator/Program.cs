using System.CommandLine;
using System.Text.Json;

using Bogus;

using MovieDatabase.FakeDataGenerator;
using MovieDatabase.FakeDataGenerator.Extensions;

const string outputPath = "./src/Api/MovieDatabase.Api.Infrastructure/Data";

var moviesFileOption = new Option<FileInfo>("--file")
{
    Description = "Path to the CSV file containing movie data",
    Required = true
};

var filmsCountOption = new Option<int>("--films-count")
{
    Description = "If > 0, generate this many films using Bogus by sampling CSV rows as templates",
    DefaultValueFactory = _ => 100,
    Required = false
};

var usersCountOption = new Option<int>("--users-count")
{
    Description = "If > 0, generate this many users; otherwise, generate one film per CSV row",
    DefaultValueFactory = _ => 10,
    Required = false
};

var rootCommand = new RootCommand("Movie Database Fake Data Generator")
{
    moviesFileOption,
    filmsCountOption,
    usersCountOption
};

var parseResult = rootCommand.Parse(args);

if (parseResult.Errors.Count > 0)
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return -1;
}

var moviesCsvPath = parseResult.GetValue(moviesFileOption)!.FullName;
var requestedCount = parseResult.GetValue(filmsCountOption);
var requestedUsersCount = parseResult.GetValue(usersCountOption);

if (!File.Exists(moviesCsvPath))
{
    Console.Error.WriteLine($"Movies file not found: {moviesCsvPath}");
    return -2;
}

var movies = FilmDataLoader.LoadMoviesFromCsv(moviesCsvPath);

if (movies.Count == 0)
{
    Console.Error.WriteLine("No movies parsed from CSV.");
    return -3;
}

var faker = new Faker();

var filmCount = requestedCount > 0 ? requestedCount : movies.Count;
var userCount = requestedUsersCount > 0 ? requestedUsersCount : filmCount;

var (filmsList, users) = faker.GenerateFilmsWithUsers(movies, filmCount, userCount);

const string filmsFileName = "films.json";
const string usersFileName = "users.json";

string outPath = Path.Join(outputPath, filmsFileName);
string usersOutPath = Path.Join(outputPath, usersFileName);

await using var fs = File.Create(outPath);
await JsonSerializer.SerializeAsync(fs, filmsList);

await using var usersStream = File.Create(usersOutPath);
await JsonSerializer.SerializeAsync(usersStream, users);

Console.WriteLine($"Wrote {filmsList.Count} films to {outPath}");
Console.WriteLine($"Wrote {users.Count} users to {usersOutPath}");

return 0;