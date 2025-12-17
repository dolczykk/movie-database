using MovieDatabase.Api.Core.Documents.Blobs;

namespace MovieDatabase.Api.Core.Services;

public interface IBlobService
{
    Task<Blob> UploadBlob(string containerName, string fileName, Stream stream,
        CancellationToken cancellationToken = default);

    string GetBlobBaseUri();
}