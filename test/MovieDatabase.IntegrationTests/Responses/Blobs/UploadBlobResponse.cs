using MovieDatabase.Api.Core.Dtos.Blobs;

namespace MovieDatabase.IntegrationTests.Responses.Blobs;

public record UploadBlobResponse(
    BlobDto UploadBlob
);

