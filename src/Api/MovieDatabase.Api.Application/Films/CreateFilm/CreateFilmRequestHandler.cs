using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Dtos.Films;
using MovieDatabase.Api.Core.Exceptions.Films;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Infrastructure.Db.Repositories;

namespace MovieDatabase.Api.Application.Films.CreateFilm;

public sealed class CreateFilmRequestHandler(IFilmRepository filmRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateFilmRequest, FilmDto>
{
    public async Task<FilmDto> HandleAsync(CreateFilmRequest request)
    {
        var filmTitle = request.Title.TrimStart().TrimEnd();
        var existingFilm = await filmRepository.GetByTitle(filmTitle);

        if (existingFilm is not null)
        {
            throw new FilmExistsApplicationException();
        }

        var film = new Film
        {
            Title = filmTitle,
            ReleaseDate = request.ReleaseDate,
            Description = request.Description?.TrimStart().TrimEnd() ?? string.Empty,
            Actors = request.Actors.Select(a => new Actor
            {
                Name = a.Name,
                Surname = a.Surname
            }).ToList(),
            Genres = request.Genres.Select(g => new Genre
            {
                Name = g.Name
            }).ToList(),
            Director = new DirectorInfo
            {
                Name = request.Director.Name,
                Surname = request.Director.Surname
            },
            Producer = new ProducerInfo
            {
                Name = request.Producer.Name
            },
            CreatorId = request.CreatorId
        };

        filmRepository.Add(film);

        await unitOfWork.Commit();

        return FilmDto.From(film);
    }
}