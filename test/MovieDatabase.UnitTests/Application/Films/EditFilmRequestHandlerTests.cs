using MovieDatabase.Api.Application.Films.EditFilm;
using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Exceptions.Films;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Infrastructure.Db.Repositories;
using MovieDatabase.UnitTests.Helpers;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Films;

public class EditFilmRequestHandlerTests
{
    private readonly IFilmRepository _mockFilmRepository;
    private readonly IUnitOfWork _mockUnitOfWork;
    private readonly EditFilmRequestHandler _handler;

    public EditFilmRequestHandlerTests()
    {
        _mockFilmRepository = Substitute.For<IFilmRepository>();
        _mockUnitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new EditFilmRequestHandler(_mockFilmRepository, _mockUnitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldUpdateFilm()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = CreateValidEditRequest(filmIdString);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe(request.Title);
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Id == filmId &&
            f.Title == request.Title));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentFilm_ShouldThrowFilmNotFoundException()
    {
        // Arrange
        var filmIdString = Guid.NewGuid().ToString();
        var request = CreateValidEditRequest(filmIdString);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(null));

        // Act
        Func<Task> act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<FilmNotExistsApplicationException>(act);

        _mockFilmRepository.DidNotReceive().Add(Arg.Any<Film>());
        await _mockUnitOfWork.DidNotReceive().Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateAllFields()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(
            id: filmId,
            title: "Old Title",
            description: "Old Description"
        );

        var request = new EditFilmRequest(
            Id: filmIdString,
            Title: "New Title",
            ReleaseDate: new DateOnly(2025, 1, 1),
            Description: "New Description",
            Actors:
            [
                new EditFilmRequest.EditFilmActorPlaceholder(null, "New", "Actor")
            ],
            Genres:
            [
                new EditFilmRequest.EditFilmGenrePlaceholder(null, "Action")
            ],
            Director: new EditFilmRequest.EditFilmDirectorPlaceholder(null, "New", "Director"),
            Producer: new EditFilmRequest.EditFilmProducerPlaceholder(null, "New Producer")
        );

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Title == "New Title" &&
            f.Description == "New Description" &&
            f.ReleaseDate == new DateOnly(2025, 1, 1) &&
            f.Actors.Count == 1 &&
            f.Actors[0].Name == "New" &&
            f.Genres.Count == 1 &&
            f.Genres[0].Name == "Action" &&
            f.Director.Name == "New" &&
            f.Producer.Name == "New Producer"));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryAddOnce()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = CreateValidEditRequest(filmIdString);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Any<Film>());
        await _mockFilmRepository.Received(1).GetById(filmIdString);
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUpdatedFilmDto()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = CreateValidEditRequest(filmIdString);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(filmIdString);
        result.Title.ShouldBe(request.Title);
        result.ReleaseDate.ShouldBe(request.ReleaseDate);
        result.Description.ShouldBe(request.Description);
        result.Actors.Count().ShouldBe(request.Actors.Length);
        result.Genres.Count().ShouldBe(request.Genres.Length);
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithExistingIds_ShouldPreserveIds()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var actorId = Guid.NewGuid().ToString();
        var genreId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();
        var producerId = Guid.NewGuid().ToString();

        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);

        var request = new EditFilmRequest(
            Id: filmIdString,
            Title: "Updated Film",
            ReleaseDate: new DateOnly(2024, 1, 1),
            Description: "Updated",
            Actors:
            [
                new EditFilmRequest.EditFilmActorPlaceholder(actorId, "Actor", "Name")
            ],
            Genres:
            [
                new EditFilmRequest.EditFilmGenrePlaceholder(genreId, "Genre")
            ],
            Director: new EditFilmRequest.EditFilmDirectorPlaceholder(directorId, "Director", "Name"),
            Producer: new EditFilmRequest.EditFilmProducerPlaceholder(producerId, "Producer")
        );

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Actors[0].Id == Guid.Parse(actorId) &&
            f.Genres[0].Id == Guid.Parse(genreId) &&
            f.Director.Id == Guid.Parse(directorId) &&
            f.Producer.Id == Guid.Parse(producerId)));
        await _mockUnitOfWork.Received(1).Commit();
    }

    private static EditFilmRequest CreateValidEditRequest(string filmId)
    {
        return new EditFilmRequest(
            Id: filmId,
            Title: "Updated Film Title",
            ReleaseDate: new DateOnly(2024, 6, 1),
            Description: "Updated Description",
            Actors:
            [
                new EditFilmRequest.EditFilmActorPlaceholder(null, "Updated", "Actor")
            ],
            Genres:
            [
                new EditFilmRequest.EditFilmGenrePlaceholder(null, "Drama")
            ],
            Director: new EditFilmRequest.EditFilmDirectorPlaceholder(null, "Updated", "Director"),
            Producer: new EditFilmRequest.EditFilmProducerPlaceholder(null, "Updated Studios")
        );
    }
}