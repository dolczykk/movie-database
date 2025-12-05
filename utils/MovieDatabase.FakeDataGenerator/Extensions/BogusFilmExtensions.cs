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
                var template = templates.Count > 0
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

        var films = faker.FilmFromTemplates(templates).Generate(count);
        if (creatorIds is not { Count: > 0 })
        {
            return films;
        }

        var creatorPool = creatorIds as IList<string> ?? creatorIds.ToList();
        foreach (var film in films)
        {
            film.CreatorId = faker.Random.ListItem(creatorPool);
        }

        return films;
    }

    internal static (List<Film> Films, List<User> Users) GenerateFilmsWithUsers(
        this Faker faker,
        IReadOnlyList<Movie> templates,
        int filmCount,
        int userCount)
    {
        var users = faker.GenerateUsersWithRoles(userCount);
        var userIds = users.Select(u => u.Id.ToString()).ToList();
        var films = faker.GenerateFilmsFromTemplates(templates, filmCount, userIds);

        return (films, users);
    }

    private static Film GenerateFilmFromTemplateCore(Faker faker, Movie template)
    {
        var createdAt = DateTime.UtcNow;

        // Generate actors randomly (ignore template cast entries)
        var actors = GenerateRandomActors(faker, createdAt);

        var genres = faker.Random.ListItems(GenrePool, faker.Random.Int(1, 3))
            .Select(name => new Genre { Id = Guid.NewGuid(), Name = name, CreatedAt = createdAt })
            .ToList();

         var title = string.IsNullOrWhiteSpace(template.Title)
            ? GenerateRandomTitle(faker)
            : template.Title!.Trim();

        var description = string.IsNullOrWhiteSpace(template.Description)
            ? faker.Lorem.Paragraph()
            : template.Description!.Trim();

        var directorName = NamePart(template.Director);
        var directorSurname = SurnamePart(template.Director);

        return new Film
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            ReleaseDate = DateOnly.FromDateTime(faker.Date.Between(DateTime.MinValue, DateTime.Now)),
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
        var descriptor = faker.Hacker.Adjective();
        var noun = faker.Random.Word();
        var textInfo = CultureInfo.InvariantCulture.TextInfo;

        return textInfo.ToTitleCase($"{descriptor} {noun}");
    }


    private static List<Actor> GenerateRandomActors(Faker faker, DateTime createdAt)
    {
        var count = faker.Random.Int(2, 5);
        
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

    private static string NamePart(string? full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return string.Empty;
        }

        var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return parts.Length > 0 ? parts[0] : full;
    }

    private static string SurnamePart(string? full)
    {
        if (string.IsNullOrWhiteSpace(full))
        {
            return string.Empty;
        }

        var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
    }
}