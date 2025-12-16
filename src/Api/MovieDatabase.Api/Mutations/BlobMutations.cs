using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using HotChocolate.Authorization;

using MovieDatabase.Api.Application.Blobs.UploadBlob;
using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Blobs;
using MovieDatabase.Api.Infrastructure.Db;

namespace MovieDatabase.Api.Mutations;

[ExtendObjectType("Mutation")]
public class BlobMutations
{
    [Authorize]
    public async Task<BlobDto> UploadBlob(ClaimsPrincipal claimsPrincipal, IFile file, [Service] IDispatcher dispatcher, [Service] IUnitOfWork unitOfWork)
    {
        var userId = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti);
        var request = new UploadBlobRequest(file, userId!.Value);
        
        var result = await dispatcher.Dispatch(request);

        await unitOfWork.Commit();
        
        return result;
    }
}
