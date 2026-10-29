using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using MovieDatabase.Api.Core.Documents.Blobs;
using MovieDatabase.SharedKernel.Configurations;

namespace MovieDatabase.Api.Core.Services;

internal class BlobService(BlobServiceClient blobClient) : IBlobService
{
    public async Task<Blob> UploadBlob(string containerName, string fileExtension, Stream stream, CancellationToken cancellationToken = default)
    {
        var container = blobClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = new Blob();

        var fileName = $"{blob.Id}{fileExtension}";
        var blobInfo = await container.UploadBlobAsync(fileName, stream, cancellationToken);

        blob.Path = $"{container.Uri.AbsolutePath}/{fileName}";
        blob.Hash = Convert.ToBase64String(blobInfo.Value.ContentHash);
        blob.Name = fileName;
        blob.BaseBlobUri = GetBlobBaseUri();

        return blob;
    }

    public string GetBlobBaseUri()
    {
        return blobClient.Uri.GetLeftPart(UriPartial.Authority);
    }

    public string GetContentUrl(string blobId)
    {
        // Built the same way `UploadBlob` builds `Blob.Path`: from the
        // container client's own `Uri.AbsolutePath`, not by hand-concatenating
        // the container name onto `GetBlobBaseUri()`. Azurite serves
        // path-style URLs (http://host:port/{account}/{container}/{blob}), so
        // `BlobServiceClient.Uri` carries the account name in its path --
        // `GetBlobBaseUri()` (scheme://host:port only) drops it, and
        // hand-concatenating the container name on top would silently
        // produce a URL missing that segment. `container.Uri.AbsolutePath`
        // already includes it (empty for production Azure, where the account
        // lives in the hostname instead).
        var container = blobClient.GetBlobContainerClient(BlobStorageConfiguration.ContentContainerName);
        return $"{GetBlobBaseUri()}{container.Uri.AbsolutePath}/{blobId}.jpg";
    }
}