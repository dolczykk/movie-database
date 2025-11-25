using System.Globalization;

using CsvHelper;

using MovieDatabase.FakeDataGenerator.Models;

namespace MovieDatabase.FakeDataGenerator;

public static class FilmDataLoader
{
    public static List<Movie> LoadMoviesFromCsv(string moviesFilePath)
    {
        using StreamReader reader = new(moviesFilePath);
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<Movie>().ToList();
    }
}