using Microsoft.EntityFrameworkCore;

using MovieDatabase.Api.Core.Documents.Films;

namespace MovieDatabase.Api.Infrastructure.Db.Repositories;

public sealed class FilmRepository(AppDbContext context) : IFilmRepository
{
    public void Add(Film film)
    {
        context.Films.Add(film);
    }

    public async Task<Film?> GetByTitle(string title)
        => await context.Films.Where(f => f.Title == title).SingleOrDefaultAsync();

    public async Task<Film?> GetById(string id)
        => await context.Films
            .Where(f => f.Id == Guid.Parse(id))
            .SingleOrDefaultAsync();

    public void Delete(Film film)
    {
        film.IsDeleted = true;

        context.Films.Update(film);
    }

    public void Update(Film film)
    {
        film.UpdatedAt = DateTime.UtcNow;

        context.Films.Update(film);
    }
}