using HotChocolate.Authorization;

using MovieDatabase.Api.Application.Users.AuthenticateUser;
using MovieDatabase.Api.Application.Users.CreateUser;
using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Users;
using MovieDatabase.Api.Infrastructure.Db;

namespace MovieDatabase.Api.Mutations;

[ExtendObjectType("Mutation")]
public class UserMutations
{
    [AllowAnonymous]
    public async Task<UserCredentialsDto> LoginUser(AuthenticateUserRequest request, [Service] IUnitOfWork unitOfWork, [Service] IDispatcher dispatcher)
    {
        var result = await dispatcher.Dispatch(request);

        await unitOfWork.Commit();

        return result;
    }

    [AllowAnonymous]
    public async Task<UserCredentialsDto> RegisterUser(CreateUserRequest request, [Service] IUnitOfWork unitOfWork, [Service] IDispatcher dispatcher)
    {
        var result = await dispatcher.Dispatch(request);

        await unitOfWork.Commit();

        return result;
    }
}