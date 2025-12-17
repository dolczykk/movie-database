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
    private string _blobBaseUri = blobService.GetBlobBaseUri();
    
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

        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var blob = await blobService.UploadBlob(Constants.Blob.ImageContainerName, fileName, request.File.OpenReadStream());

        blob.UserId = request.UserId;

        await blobRepository.Add(blob);

        var path = _blobBaseUri + blob.Path;
        blob.Path = path;
        
        return BlobDto.From(blob);
    }
}