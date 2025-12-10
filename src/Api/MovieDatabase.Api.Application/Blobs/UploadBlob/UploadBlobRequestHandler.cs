using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Blobs;
using MovieDatabase.Api.Core.Services;
using MovieDatabase.Api.Infrastructure.Db.Repositories;

namespace MovieDatabase.Api.Application.Blobs.UploadBlob;

public class UploadBlobRequestHandler(
    IBlobService blobService,
    IBlobRepository blobRepository
) : IRequestHandler<UploadBlobRequest, BlobDto>
{
    public async Task<BlobDto> HandleAsync(UploadBlobRequest request)
    {
        var blob = await blobService.UploadBlob("files", request.File.Name, request.File.OpenReadStream());

        await blobRepository.Add(blob);

        return BlobDto.From(blob);
    }
}