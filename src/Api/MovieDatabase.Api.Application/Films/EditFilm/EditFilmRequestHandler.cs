using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Dtos.Films;
using MovieDatabase.Api.Core.Exceptions.Films;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Infrastructure.Db.Repositories;

namespace MovieDatabase.Api.Application.Films.EditFilm;

public sealed class EditFilmRequestHandler(IFilmRepository filmRepository, IUnitOfWork unitOfWork) : IRequestHandler<EditFilmRequest, FilmDto>
{
    public async Task<FilmDto> HandleAsync(EditFilmRequest request)
    {
        var film = await filmRepository.GetById(request.Id);
        if (film is null)
        {
            throw new FilmNotExistsApplicationException();
        }

        film.Title = request.Title;
        film.Description = request.Description;
        film.ReleaseDate = request.ReleaseDate;

        // Unlike every other field here, Thumbnail is not unconditionally
        // overwritten: it's only sent when the visitor picked a new image
        // this edit session (there's no "remove thumbnail" UI), so an
        // omitted/null value must preserve whatever the film already has.
        if (request.Thumbnail is not null)
        {
            film.Thumbnail = request.Thumbnail;
        }

        film.Director = new DirectorInfo
        {
            Id = request.Director.Id != null ? Guid.Parse(request.Director.Id) : Guid.NewGuid(),
            Name = request.Director.Name!,
            Surname = request.Director.Surname!
        };

        film.Producer = new ProducerInfo
        {
            Id = request.Producer.Id != null ? Guid.Parse(request.Producer.Id) : Guid.NewGuid(),
            Name = request.Producer.Name!
        };

        film.Actors = request.Actors.Select(a => new Actor
        {
            Id = a.Id != null ? Guid.Parse(a.Id) : Guid.NewGuid(),
            Name = a.Name!,
            Surname = a.Surname!
        }).ToList();

        film.Genres = request.Genres.Select(g => new Genre
        {
            Id = g.Id != null ? Guid.Parse(g.Id) : Guid.NewGuid(),
            Name = g.Name!
        }).ToList();

        filmRepository.Update(film);

        await unitOfWork.Commit();

        return FilmDto.From(film);
    }
}
