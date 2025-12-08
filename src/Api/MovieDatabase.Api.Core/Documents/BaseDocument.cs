using HotChocolate;

namespace MovieDatabase.Api.Core.Documents;

public abstract class BaseDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [GraphQLIgnore]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [GraphQLIgnore]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [GraphQLIgnore]
    public bool IsDeleted { get; set; } = false;
}