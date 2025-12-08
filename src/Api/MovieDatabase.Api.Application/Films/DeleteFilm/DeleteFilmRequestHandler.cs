using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Exceptions.Films;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Infrastructure.Db.Repositories;

namespace MovieDatabase.Api.Application.Films.DeleteFilm;

public sealed class DeleteFilmRequestHandler(IFilmRepository filmRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteFilmRequest, string>
{
    public async Task<string> HandleAsync(DeleteFilmRequest request)
    {
        var film = await filmRepository.GetById(request.FilmId);

        if (film is null)
        {
            throw new FilmNotExistsApplicationException();
        }

        filmRepository.Delete(film);

        await unitOfWork.Commit();

        return film.Id.ToString();
    }
}