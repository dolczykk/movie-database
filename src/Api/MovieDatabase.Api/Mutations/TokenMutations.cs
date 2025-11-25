using HotChocolate.Authorization;

using MovieDatabase.Api.Application.Users.RevokeToken;
using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Users;
using MovieDatabase.Api.Infrastructure.Db;

namespace MovieDatabase.Api.Mutations;

[ExtendObjectType("Mutation")]
public class TokenMutations
{
    [AllowAnonymous]
    public async Task<RevokeTokenDto> Revoke(RevokeTokenRequest input, [Service] IUnitOfWork unitOfWork, [Service] IDispatcher dispatcher)
    {
        var result = await dispatcher.Dispatch(input);

        await unitOfWork.Commit();
        
        return result;
    }
}