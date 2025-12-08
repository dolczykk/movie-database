using MovieDatabase.Api.Application.Users.CreateUser;
using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Documents.Users;

namespace MovieDatabase.UnitTests.Helpers;

public static class TestDataBuilder
{
    public static User CreateValidUser(
        Guid? id = null,
        string? name = null,
        string? email = null,
        string? passwordHash = null,
        UserRoles? role = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "TestUser",
            Email = email ?? "test@example.com",
            PasswordHash = passwordHash ?? "$2a$11$TestHashValue",
            Role = role ?? UserRoles.User
        };
    }

    public static CreateUserRequest CreateValidCreateUserRequest(
        string? username = null,
        string? email = null,
        string? password = null)
    {
        return new CreateUserRequest(
            Username: username ?? "TestUser",
            Email: email ?? "test@example.com",
            Password: password ?? "SecurePassword123!"
        );
    }

    public static Film CreateValidFilm(
        Guid? id = null,
        string? title = null,
        string? description = null,
        DateOnly? releaseDate = null,
        List<Actor>? actors = null,
        List<Genre>? genres = null,
        DirectorInfo? director = null,
        ProducerInfo? producer = null,
        string? creatorId = null)
    {
        return new Film
        {
            Id = id ?? Guid.NewGuid(),
            Title = title ?? "Test Film",
            Description = description ?? "Test description",
            ReleaseDate = releaseDate ?? new DateOnly(2024, 1, 1),
            Actors = actors ?? [new Actor { Name = "Test", Surname = "Actor" }],
            Genres = genres ?? [new Genre { Name = "Drama" }],
            Director = director ?? new DirectorInfo { Name = "Test", Surname = "Director" },
            Producer = producer ?? new ProducerInfo { Name = "Test Studios" },
            CreatorId = creatorId ?? Guid.NewGuid().ToString()
        };
    }
}