using System.Text.Json.Serialization;

using HotChocolate;

namespace MovieDatabase.Api.Core.Documents.Blobs;

public class Blob : BaseDocument
{
    [JsonIgnore]
    public const string PartitionKey = "/Id";
    
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Hash { get; set; } = null!;
    
    [JsonIgnore]
    [GraphQLIgnore]
    public string BaseBlobUri { get; set; } = null!;
    
    public string GetFullPath()
        => $"{BaseBlobUri}{Path}";
}