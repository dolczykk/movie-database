using MovieDatabase.Api.Core.Documents.Blobs;
using MovieDatabase.Api.Core.Interfaces;

namespace MovieDatabase.Api.Core.Dtos.Blobs;

public record BlobDto(
    string Id,
    string FileName,
    string Url) : IFrom<BlobDto, Blob>
{
    public static BlobDto From(Blob from)
        => new BlobDto(from.Id.ToString(), from.Name, from.Path);
}