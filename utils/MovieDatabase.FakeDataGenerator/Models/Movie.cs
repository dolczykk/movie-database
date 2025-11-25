using CsvHelper.Configuration.Attributes;

namespace MovieDatabase.FakeDataGenerator.Models;

public class Movie
{
    [Name("title")] public string? Title { get; set; }

    [Name("director")] public string? Director { get; set; }

    [Name("cast")] public string? Cast { get; set; }

    [Name("date_added")] public DateTime? DateAdded { get; set; }

    [Name("description")] public string? Description { get; set; }
}