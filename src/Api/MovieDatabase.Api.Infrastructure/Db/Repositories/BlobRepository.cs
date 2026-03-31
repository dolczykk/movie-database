using MovieDatabase.Api.Core.Documents.Blobs;

namespace MovieDatabase.Api.Infrastructure.Db.Repositories;

public sealed class BlobRepository(AppDbContext context) : IBlobRepository
{
    public async Task Add(Blob document)
    {
        await context.Blobs.AddAsync(document);
    }
    
    public async Task<Blob?> GetById(Guid id)
    {
        return await context.Blobs.FindAsync(id);
    }
}