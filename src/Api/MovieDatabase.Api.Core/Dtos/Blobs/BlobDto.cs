using MovieDatabase.Api.Core.Documents.Blobs;
using MovieDatabase.Api.Core.Interfaces;

namespace MovieDatabase.Api.Core.Dtos.Blobs;

public record BlobDto(
    string Id,
    string FileName,
    string Url,
    string Hash) : IFrom<BlobDto, Blob>
{
    public static BlobDto From(Blob from) 
        => new(from.Id.ToString(), from.Name, from.GetFullPath(), from.Hash);
}