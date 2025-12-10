using HotChocolate.Types;

using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Blobs;

namespace MovieDatabase.Api.Application.Blobs.UploadBlob;

public record UploadBlobRequest(IFile File, string UserId) : IRequest<BlobDto>;
