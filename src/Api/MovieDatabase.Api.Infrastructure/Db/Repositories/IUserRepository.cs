using MovieDatabase.Api.Core.Documents.Users;

namespace MovieDatabase.Api.Infrastructure.Db.Repositories;

public interface IUserRepository
{
    void Add(User user);
    void Update(User user);
    Task<User?> GetByEmail(string email);
    Task<User?> GetById(string id);
    Task<User?> FindUserToRevokeToken(string userId, string accessToken, string refreshToken);
}