using System.Globalization;

using CsvHelper;

using MovieDatabase.FakeDataGenerator.Models;

namespace MovieDatabase.FakeDataGenerator;

internal static class FilmDataLoader
{
    internal static List<Movie> LoadMoviesFromCsv(string moviesFilePath)
    {
        using var reader = new StreamReader(moviesFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<Movie>().ToList();
    }
}