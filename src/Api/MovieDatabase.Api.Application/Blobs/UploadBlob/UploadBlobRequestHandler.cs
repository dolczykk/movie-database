using MovieDatabase.Api.Core;
using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Blobs;
using MovieDatabase.Api.Core.Exceptions.Blobs;
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
        if (!Constants.Blob.AllowedContentTypes.Contains(request.File.ContentType))
        {
            throw new NotSupportedContentTypeApplicationException();
        }
        
        var fileExtension = Path.GetExtension(request.File.Name);
        if (fileExtension == null)
        {
            throw new NotSupportedContentTypeApplicationException();
        }
        
        var stream = request.File.OpenReadStream();
        var blob = await blobService.UploadBlob(Constants.Blob.ImageContainerName, fileExtension, stream);
        
        await stream.DisposeAsync();

        blob.UserId = request.UserId;

        await blobRepository.Add(blob);

        return BlobDto.From(blob);
    }
}