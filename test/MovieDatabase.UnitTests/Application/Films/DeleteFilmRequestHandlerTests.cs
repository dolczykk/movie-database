using MovieDatabase.Api.Application.Films.DeleteFilm;
using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Exceptions.Films;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Infrastructure.Db.Repositories;
using MovieDatabase.UnitTests.Helpers;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Films;

public class DeleteFilmRequestHandlerTests
{
    private readonly IFilmRepository _mockFilmRepository;
    private readonly IUnitOfWork _mockUnitOfWork;
    private readonly DeleteFilmRequestHandler _handler;

    public DeleteFilmRequestHandlerTests()
    {
        _mockFilmRepository = Substitute.For<IFilmRepository>();
        _mockUnitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new DeleteFilmRequestHandler(_mockFilmRepository, _mockUnitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithExistingFilm_ShouldDeleteFilm()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var userId = Guid.NewGuid().ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = new DeleteFilmRequest(filmIdString, userId);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldBe(filmIdString);
        _mockFilmRepository.Received(1).Delete(Arg.Is<Film>(f => f.Id == filmId));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentFilm_ShouldThrowFilmNotFoundException()
    {
        // Arrange
        var filmIdString = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var request = new DeleteFilmRequest(filmIdString, userId);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(null));

        // Act
        Func<Task> act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<FilmNotExistsApplicationException>(act);

        _mockFilmRepository.DidNotReceive().Delete(Arg.Any<Film>());
        await _mockUnitOfWork.DidNotReceive().Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryDeleteOnce()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var userId = Guid.NewGuid().ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = new DeleteFilmRequest(filmIdString, userId);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Delete(Arg.Any<Film>());
        await _mockFilmRepository.Received(1).GetById(filmIdString);
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFilmId()
    {
        // Arrange
        var filmId = Guid.NewGuid();
        var filmIdString = filmId.ToString();
        var userId = Guid.NewGuid().ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = new DeleteFilmRequest(filmIdString, userId);

        _mockFilmRepository.GetById(filmIdString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe(filmIdString);
        Guid.TryParse(result, out _).ShouldBeTrue("result should be a valid GUID string");
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("12345678-1234-1234-1234-123456789012")]
    [InlineData("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    public async Task HandleAsync_WithVariousFilmIds_ShouldHandleCorrectly(string guidString)
    {
        // Arrange
        var filmId = Guid.Parse(guidString);
        var userId = Guid.NewGuid().ToString();
        var existingFilm = TestDataBuilder.CreateValidFilm(id: filmId);
        var request = new DeleteFilmRequest(guidString, userId);

        _mockFilmRepository.GetById(guidString)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldBe(guidString);
        _mockFilmRepository.Received(1).Delete(Arg.Is<Film>(f => f.Id == filmId));
        await _mockUnitOfWork.Received(1).Commit();
    }
}