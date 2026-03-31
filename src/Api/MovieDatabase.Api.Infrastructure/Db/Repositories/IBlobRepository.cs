using MovieDatabase.Api.Core.Documents.Blobs;

namespace MovieDatabase.Api.Infrastructure.Db.Repositories;

public interface IBlobRepository
{
    Task Add(Blob document);
    Task<Blob?> GetById(Guid id);
}