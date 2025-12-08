using MovieDatabase.Api.Application.Films.CreateFilm;
using MovieDatabase.Api.Core.Documents.Films;
using MovieDatabase.Api.Core.Exceptions.Films;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Infrastructure.Db.Repositories;
using MovieDatabase.UnitTests.Helpers;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Films;

public class CreateFilmRequestHandlerTests
{
    private readonly IFilmRepository _mockFilmRepository;
    private readonly IUnitOfWork _mockUnitOfWork;
    private readonly CreateFilmRequestHandler _handler;

    public CreateFilmRequestHandlerTests()
    {
        _mockFilmRepository = Substitute.For<IFilmRepository>();
        _mockUnitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateFilmRequestHandler(_mockFilmRepository, _mockUnitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldCreateFilm()
    {
        // Arrange
        var request = CreateValidRequest();
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Title == request.Title &&
            f.ReleaseDate == request.ReleaseDate));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldReturnFilmDto()
    {
        // Arrange
        var request = CreateValidRequest();
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe(request.Title);
        result.ReleaseDate.ShouldBe(request.ReleaseDate);
        result.Description.ShouldBe(request.Description);
        result.Actors.Length.ShouldBe(request.Actors.Length);
        result.Genres.Length.ShouldBe(request.Genres.Length);
        result.Director.ShouldNotBeNull();
        result.Producer.ShouldNotBeNull();
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateTitle_ShouldThrowFilmExistsException()
    {
        // Arrange
        var request = CreateValidRequest();
        var existingFilm = TestDataBuilder.CreateValidFilm(title: request.Title);

        _mockFilmRepository.GetByTitle(request.Title)
            .Returns(Task.FromResult<Film?>(existingFilm));

        // Act
        Func<Task> act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<FilmExistsApplicationException>(act);

        _mockFilmRepository.DidNotReceive().Add(Arg.Any<Film>());
        await _mockUnitOfWork.DidNotReceive().Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimTitleWhitespace()
    {
        // Arrange
        var request = new CreateFilmRequest(
            Title: "  Test Film  ",
            ReleaseDate: new DateOnly(2024, 1, 1),
            Description: "Test",
            Actors: new[] { new CreateFilmRequest.ActorPlaceholder(null, "John", "Doe") },
            Genres: new[] { new CreateFilmRequest.GenrePlaceholder(null, "Drama") },
            Director: new CreateFilmRequest.DirectorPlaceholder(null, "Jane", "Smith"),
            Producer: new CreateFilmRequest.ProducerPlaceholder(null, "Test Studios")
        )
        { CreatorId = Guid.NewGuid().ToString() };

        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Title == "Test Film" &&
            !f.Title.StartsWith(" ") &&
            !f.Title.EndsWith(" ")));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldTrimDescriptionWhitespace()
    {
        // Arrange
        var request = CreateValidRequest() with { Description = "  Test Description  " };
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Description == "Test Description" &&
            !f.Description.StartsWith(" ") &&
            !f.Description.EndsWith(" ")));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithNullDescription_ShouldSetEmptyString()
    {
        // Arrange
        var request = CreateValidRequest() with { Description = null };
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Description == string.Empty));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldMapActorsCorrectly()
    {
        // Arrange
        var request = new CreateFilmRequest(
            Title: "Test Film",
            ReleaseDate: new DateOnly(2024, 1, 1),
            Description: "Test",
            Actors: new[]
            {
                new CreateFilmRequest.ActorPlaceholder(null, "John", "Doe"),
                new CreateFilmRequest.ActorPlaceholder(null, "Jane", "Smith")
            },
            Genres: new[] { new CreateFilmRequest.GenrePlaceholder(null, "Drama") },
            Director: new CreateFilmRequest.DirectorPlaceholder(null, "Director", "Name"),
            Producer: new CreateFilmRequest.ProducerPlaceholder(null, "Producer")
        )
        { CreatorId = Guid.NewGuid().ToString() };

        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Actors.Count == 2 &&
            f.Actors[0].Name == "John" &&
            f.Actors[0].Surname == "Doe" &&
            f.Actors[1].Name == "Jane" &&
            f.Actors[1].Surname == "Smith"));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldMapGenresCorrectly()
    {
        // Arrange
        var request = new CreateFilmRequest(
            "Test Film",
            new DateOnly(2024, 1, 1),
             "Test",
            Actors: [new CreateFilmRequest.ActorPlaceholder(null, "John", "Doe")],
            Genres:
            [
                new CreateFilmRequest.GenrePlaceholder(null, "Drama"),
                new CreateFilmRequest.GenrePlaceholder(null, "Thriller")
            ],
            Director: new CreateFilmRequest.DirectorPlaceholder(null, "Director", "Name"),
            Producer: new CreateFilmRequest.ProducerPlaceholder(null, "Producer")
        )
        { CreatorId = Guid.NewGuid().ToString() };

        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Genres.Count == 2 &&
            f.Genres[0].Name == "Drama" &&
            f.Genres[1].Name == "Thriller"));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldMapDirectorCorrectly()
    {
        // Arrange
        var request = CreateValidRequest();
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Director.Name == request.Director.Name &&
            f.Director.Surname == request.Director.Surname));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldMapProducerCorrectly()
    {
        // Arrange
        var request = CreateValidRequest();
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.Producer.Name == request.Producer.Name));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCreatorId()
    {
        // Arrange
        var creatorId = Guid.NewGuid().ToString();
        var request = CreateValidRequest() with { CreatorId = creatorId };
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Is<Film>(f =>
            f.CreatorId == creatorId));
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallRepositoryAddOnce()
    {
        // Arrange
        var request = CreateValidRequest();
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        _mockFilmRepository.Received(1).Add(Arg.Any<Film>());
        await _mockFilmRepository.Received(1).GetByTitle(Arg.Any<string>());
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Theory]
    [InlineData("The Matrix")]
    [InlineData("Inception")]
    [InlineData("The Shawshank Redemption")]
    public async Task HandleAsync_WithVariousTitles_ShouldSucceed(string title)
    {
        // Arrange
        var request = CreateValidRequest() with { Title = title };
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe(title);
        await _mockUnitOfWork.Received(1).Commit();
    }

    [Fact]
    public async Task HandleAsync_WithEmptyActorsList_ShouldCreateFilmWithNoActors()
    {
        // Arrange
        var request = CreateValidRequest() with
        {
            Actors = []
        };
        _mockFilmRepository.GetByTitle(Arg.Any<string>())
            .Returns(Task.FromResult<Film?>(null));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Actors.ShouldBeEmpty();
        await _mockUnitOfWork.Received(1).Commit();
    }

    private static CreateFilmRequest CreateValidRequest()
    {
        return new CreateFilmRequest(
            Title: "Test Film",
            ReleaseDate: new DateOnly(2024, 1, 1),
            Description: "Test Description",
            Actors:
            [
                new CreateFilmRequest.ActorPlaceholder(null, "John", "Doe")
            ],
            Genres:
            [
                new CreateFilmRequest.GenrePlaceholder(null, "Drama")
            ],
            Director: new CreateFilmRequest.DirectorPlaceholder(null, "Jane", "Smith"),
            Producer: new CreateFilmRequest.ProducerPlaceholder(null, "Test Studios")
        )
        {
            CreatorId = Guid.NewGuid().ToString()
        };
    }
}