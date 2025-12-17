using System.Text.Json.Serialization;

namespace MovieDatabase.Api.Core.Documents.Blobs;

public class Blob : BaseDocument
{
    [JsonIgnore]
    public const string PartitionKey = "/Name";
    
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Hash { get; set; } = null!;
}