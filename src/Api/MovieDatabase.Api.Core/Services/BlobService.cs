using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using MovieDatabase.Api.Core.Documents.Blobs;

namespace MovieDatabase.Api.Core.Services;

internal class BlobService(BlobServiceClient blobClient) : IBlobService
{
    public async Task<Blob> UploadBlob(string containerName, string fileName, Stream stream, CancellationToken cancellationToken = default)
    {
        var container = blobClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobInfo = await container.UploadBlobAsync(fileName, stream, cancellationToken);

        return new Blob { Name = fileName, Path = $"{container.Uri.AbsolutePath}/{fileName}", };
    }
}