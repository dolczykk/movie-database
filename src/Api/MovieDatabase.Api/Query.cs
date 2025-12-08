using Microsoft.EntityFrameworkCore;

using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Infrastructure.Db;

namespace MovieDatabase.Api;

public class Query
{
    private static IQueryable<Film> BaseQuery(AppDbContext dbContext)
        => dbContext.Films
            .AsNoTracking();

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Film> Films(
        [Service] AppDbContext dbContext)
        => BaseQuery(dbContext);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Actor> Actors(
        [Service] AppDbContext dbContext)
        => BaseQuery(dbContext)
            .SelectMany(f => f.Actors);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Genre> Genres(
        [Service] AppDbContext dbContext)
        => BaseQuery(dbContext)
            .SelectMany(f => f.Genres);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<DirectorInfo> Directors(
        [Service] AppDbContext dbContext)
        => BaseQuery(dbContext)
            .Select(f => f.Director);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ProducerInfo> Producers(
        [Service] AppDbContext dbContext)
        => BaseQuery(dbContext)
            .Select(f => f.Producer);
}