using Microsoft.EntityFrameworkCore;

using User = MovieDatabase.Api.Core.Documents.Users.User;

namespace MovieDatabase.Api.Infrastructure.Db.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public void Add(User user)
    {
        context.Users.Add(user);
    }

    public void Update(User user)
    {
        context.Users.Update(user);
    }

    public async Task<User?> GetByEmail(string email)
        => await context.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetById(string id)
        => await context.Users
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(id));

    public async Task<User?> FindUserToRevokeToken(string userId, string accessToken, string refreshToken)
        => await context.Users
            .Include(u => u.Tokens)
            .Where(u => u.Id == Guid.Parse(userId) && u.Tokens.Any(
                    t => !t.IsRevoked && t.AccessToken == accessToken && t.RefreshToken == refreshToken && t.ExpiresAt > DateTime.UtcNow
            ))
            .SingleOrDefaultAsync();
}