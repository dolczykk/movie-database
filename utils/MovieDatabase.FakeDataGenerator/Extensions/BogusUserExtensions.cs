using Bogus;

using MovieDatabase.Api.Core.Documents.Users;
using MovieDatabase.Api.Core.Utils;

namespace MovieDatabase.FakeDataGenerator.Extensions;

internal static class BogusUserExtensions
{
    private const int MinModerators = 1;

    internal static List<User> GenerateUsersWithRoles(
        this Faker faker,
        int totalUsers,
        int maxModerators = 5)
    {
        var userTotal = Math.Max(totalUsers, 1);
        var users = new List<User>(userTotal) { CreateUser(UserRoles.Administrator, faker) };

        var remainingSlots = userTotal - 1;
        var moderatorSlots = Math.Max(0, Math.Min(maxModerators, remainingSlots));

        var moderatorLowerBound = moderatorSlots > 0 ? Math.Clamp(MinModerators, 0, moderatorSlots) : 0;

        var moderatorCount = moderatorSlots > 0
            ? faker.Random.Int(moderatorLowerBound, moderatorSlots)
            : 0;

        for (var i = 0; i < moderatorCount; i++)
        {
            users.Add(CreateUser(UserRoles.Moderator, faker));
        }

        remainingSlots -= moderatorCount;

        for (var i = 0; i < remainingSlots; i++)
        {
            users.Add(CreateUser(UserRoles.User, faker));
        }

        return users;
    }

    private static User CreateUser(UserRoles role, Faker faker) 
        => new()
        {
            Id = Guid.NewGuid(),
            Name = faker.Internet.UserName(),
            PasswordHash = PasswordUtils.HashPassword("example123!"),
            Email = faker.Internet.ExampleEmail(),
            CreatedAt = DateTime.UtcNow,
            Role = role,
            Tokens = []
        };
}