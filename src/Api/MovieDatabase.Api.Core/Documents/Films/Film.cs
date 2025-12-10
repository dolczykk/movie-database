using System.Text.Json.Serialization;

using MovieDatabase.Api.Core.Documents.Blobs;

namespace MovieDatabase.Api.Core.Documents.Films;

public class Film : BaseDocument
{
    [JsonIgnore]
    public const string PartitionKey = "/title";

    public string Title { get; set; } = null!;
    public string? Thumbnail { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public DirectorInfo Director { get; set; }
    public List<Actor> Actors { get; set; }
    public List<Genre> Genres { get; set; }
    public ProducerInfo Producer { get; set; }
    public string? Description { get; set; }
    public string CreatorId { get; set; }
}