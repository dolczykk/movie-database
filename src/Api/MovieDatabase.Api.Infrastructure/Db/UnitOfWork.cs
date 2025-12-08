namespace MovieDatabase.Api.Infrastructure.Db;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    public async Task Commit()
    {
        await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}