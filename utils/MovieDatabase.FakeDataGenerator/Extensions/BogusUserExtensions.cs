using Bogus;

using MovieDatabase.Api.Core.Documents.Users;

namespace MovieDatabase.FakeDataGenerator.Extensions;

internal static class BogusUserExtensions
{
    public static List<User> GenerateUsersWithRoles(
        this Faker faker,
        int totalUsers,
        int minModerators = 1,
        int maxModerators = 5)
    {
        if (minModerators > maxModerators && maxModerators > 0)
        {
            (minModerators, maxModerators) = (maxModerators, minModerators);
        }

        int userTotal = Math.Max(totalUsers, 1);
        List<User> users = new(userTotal) { CreateUser(UserRoles.Administrator) };

        int remainingSlots = userTotal - 1;
        int moderatorSlots = Math.Max(0, Math.Min(maxModerators, remainingSlots));

        int moderatorLowerBound = moderatorSlots > 0
            ? Math.Clamp(minModerators, 0, moderatorSlots)
            : 0;

        int moderatorCount = moderatorSlots > 0
            ? faker.Random.Int(moderatorLowerBound, moderatorSlots)
            : 0;

        for (int i = 0; i < moderatorCount; i++)
        {
            users.Add(CreateUser(UserRoles.Moderator));
        }

        remainingSlots -= moderatorCount;

        for (int i = 0; i < remainingSlots; i++)
        {
            users.Add(CreateUser(UserRoles.User));
        }

        return users;
    }

    private static User CreateUser(UserRoles role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            PasswordHash = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid()}@example.com",
            CreatedAt = DateTime.UtcNow,
            Role = role,
            Tokens = []
        };
    }
}