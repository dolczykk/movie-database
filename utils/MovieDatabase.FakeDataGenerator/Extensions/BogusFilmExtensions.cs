using System.Globalization;

using Bogus;

using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.FakeDataGenerator.Models;

namespace MovieDatabase.FakeDataGenerator.Extensions;

internal static class BogusFilmExtensions
{
    private static readonly string[] GenrePool =
    [
        "Drama",
        "Romance",
        "Mystery",
        "Sci-Fi",
        "Adventure",
        "Fantasy",
        "Thriller",
        "Comedy"
    ];

    private static Faker<Film> FilmFromTemplates(this Faker faker, IReadOnlyList<Movie> templates)
    {
        return new Faker<Film>(faker.Locale)
            .CustomInstantiator(fkr =>
            {
                Movie template = templates.Count > 0
                    ? templates[fkr.Random.Int(0, templates.Count - 1)]
                    : new Movie();
                return GenerateFilmFromTemplateCore(fkr, template);
            });
    }

    private static List<Film> GenerateFilmsFromTemplates(this Faker faker, IReadOnlyList<Movie> templates, int count,
        IReadOnlyList<string>? creatorIds = null)
    {
        if (count == 0)
        {
            return [];
        }

        List<Film>? films = faker.FilmFromTemplates(templates).Generate(count);

        if (creatorIds is not { Count: > 0 })
        {
            return films;
        }

        IList<string> creatorPool = creatorIds as IList<string> ?? creatorIds.ToList();
        foreach (Film film in films)
        {
            film.CreatorId = faker.Random.ListItem(creatorPool);
        }

        return films;
    }

    public static (List<Film> Films, List<User> Users) GenerateFilmsWithUsers(
        this Faker faker,
        IReadOnlyList<Movie> templates,
        int filmCount,
        int userCount = 100)
    {
        List<User> users = faker.GenerateUsersWithRoles(userCount);
        List<string> userIds = users.Select(u => u.Id.ToString()).ToList();
        List<Film> films = faker.GenerateFilmsFromTemplates(templates, filmCount, userIds);

        return (films, users);
    }

    private static Film GenerateFilmFromTemplateCore(Faker faker, Movie template)
    {
        DateTime createdAt = DateTime.UtcNow;
        DateOnly releaseDate = template.DateAdded is not null
            ? DateOnly.FromDateTime(template.DateAdded.Value)
            : DateOnly.FromDateTime(faker.Date.Past(35));

        List<string> castEntries = ParseCast(template);
        List<Actor> actors = castEntries.Count > 0
            ? castEntries
                .Select(full => new Actor
                {
                    Id = Guid.NewGuid(),
                    Name = FilmUtils.NamePart(full),
                    Surname = FilmUtils.SurnamePart(full),
                    CreatedAt = createdAt
                })
                .ToList()
            : GenerateRandomActors(faker, createdAt);

        List<Genre> genres = faker.Random.ListItems(GenrePool, faker.Random.Int(1, 3))
            .Select(name => new Genre { Id = Guid.NewGuid(), Name = name, CreatedAt = createdAt })
            .ToList();

        string title = string.IsNullOrWhiteSpace(template.Title)
            ? GenerateRandomTitle(faker)
            : template.Title!.Trim();

        string? description = string.IsNullOrWhiteSpace(template.Description)
            ? faker.Lorem.Paragraph()
            : template.Description!.Trim();

        string directorName = FilmUtils.NamePart(template.Director);
        string directorSurname = FilmUtils.SurnamePart(template.Director);

        return new Film
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            ReleaseDate = releaseDate,
            CreatedAt = createdAt,
            CreatorId = Guid.NewGuid().ToString(),
            Director = new DirectorInfo
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(directorName) ? faker.Name.FirstName() : directorName,
                Surname = string.IsNullOrWhiteSpace(directorSurname) ? faker.Name.LastName() : directorSurname,
                CreatedAt = createdAt
            },
            Actors = actors,
            Genres = genres,
            Producer = new ProducerInfo
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(directorName)
                    ? faker.Company.CompanyName()
                    : directorName,
                CreatedAt = createdAt
            }
        };
    }

    private static string GenerateRandomTitle(Faker faker)
    {
        string? descriptor = faker.Hacker.Adjective();
        string? noun = faker.Random.Word();
        string? suffix = faker.Random.ArrayElement(["Chronicles", "Project", "Memoir", "Odyssey", "Protocol"]);
        TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

        return textInfo.ToTitleCase($"{descriptor} {noun} {suffix}");
    }

    private static List<string> ParseCast(Movie template)
    {
        return (template.Cast ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static List<Actor> GenerateRandomActors(Faker faker, DateTime createdAt)
    {
        int count = faker.Random.Int(2, 5);
        return Enumerable.Range(0, count)
            .Select(_ => new Actor
            {
                Id = Guid.NewGuid(),
                Name = faker.Name.FirstName(),
                Surname = faker.Name.LastName(),
                CreatedAt = createdAt
            })
            .ToList();
    }
}