using MovieDatabase.Api.Core.Documents.Films;

namespace MovieDatabase.Api.Infrastructure.Db.Repositories;

public interface IFilmRepository
{
    void Add(Film film);
    Task<Film?> GetByTitle(string title);
    Task<Film?> GetById(string id);
    void Delete(Film film);
    void Update(Film film);
}