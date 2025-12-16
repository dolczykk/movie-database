using HotChocolate.Types;

using MovieDatabase.Api.Application.Blobs.UploadBlob;
using MovieDatabase.Api.Core.Documents.Blobs;
using MovieDatabase.Api.Core.Exceptions.Blobs;
using MovieDatabase.Api.Core.Services;
using MovieDatabase.Api.Infrastructure.Db.Repositories;

using NSubstitute;

using Shouldly;

namespace MovieDatabase.UnitTests.Application.Blobs;

public class UploadBlobRequestHandlerTests
{
    private readonly IBlobService _mockBlobService;
    private readonly IBlobRepository _mockBlobRepository;
    private readonly UploadBlobRequestHandler _handler;

    public UploadBlobRequestHandlerTests()
    {
        _mockBlobService = Substitute.For<IBlobService>();
        _mockBlobRepository = Substitute.For<IBlobRepository>();
        _handler = new UploadBlobRequestHandler(_mockBlobService, _mockBlobRepository);
    }

    [Fact]
    public async Task HandleAsync_WithValidImageFile_ShouldUploadAndReturnBlobDto()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "test-image.png";
        var contentType = "image/png";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        var expectedBlob = new Blob
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            Path = $"files/{fileName}",
            UserId = userId
        };

        _mockBlobService.UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedBlob));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(expectedBlob.Id.ToString());
        result.FileName.ShouldBe(expectedBlob.Name);
        result.Url.ShouldBe(expectedBlob.Path);

        await _mockBlobService.Received(1).UploadBlob(
            "files",
            fileName,
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());

        await _mockBlobRepository.Received(1).Add(Arg.Is<Blob>(b =>
            b.Id == expectedBlob.Id &&
            b.Name == expectedBlob.Name));
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/jpg")]
    [InlineData("image/webp")]
    public async Task HandleAsync_WithAllowedContentTypes_ShouldSucceed(string contentType)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "test-image.png";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        var expectedBlob = new Blob
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            Path = $"files/{fileName}",
            UserId = userId
        };

        _mockBlobService.UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedBlob));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.ShouldNotBeNull();
        await _mockBlobService.Received(1).UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("application/json")]
    [InlineData("video/mp4")]
    [InlineData("audio/mpeg")]
    [InlineData("image/gif")]
    [InlineData("image/svg+xml")]
    public async Task HandleAsync_WithNotAllowedContentType_ShouldThrowNotSupportedContentTypeException(string contentType)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "test-file.pdf";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        // Act
        Func<Task> act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<NotSupportedContentTypeApplicationException>(act);

        await _mockBlobService.DidNotReceive().UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());

        await _mockBlobRepository.DidNotReceive().Add(Arg.Any<Blob>());
    }

    [Fact]
    public async Task HandleAsync_ShouldUploadToFilesContainer()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "test-image.jpeg";
        var contentType = "image/jpeg";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        var expectedBlob = new Blob
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            Path = $"files/{fileName}",
            UserId = userId
        };

        _mockBlobService.UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedBlob));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        await _mockBlobService.Received(1).UploadBlob(
            "files",
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldAddBlobToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "test-image.webp";
        var contentType = "image/webp";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        var expectedBlob = new Blob
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            Path = $"files/{fileName}",
            UserId = userId
        };

        _mockBlobService.UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedBlob));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        await _mockBlobRepository.Received(1).Add(Arg.Is<Blob>(b =>
            b.Id == expectedBlob.Id &&
            b.Name == expectedBlob.Name &&
            b.Path == expectedBlob.Path));
    }

    [Fact]
    public async Task HandleAsync_WhenBlobServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "test-image.png";
        var contentType = "image/png";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        _mockBlobService.UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>())
            .Returns<Blob>(x => throw new InvalidOperationException("Blob storage unavailable"));

        // Act
        Func<Task> act = () => _handler.HandleAsync(request);

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);

        await _mockBlobRepository.DidNotReceive().Add(Arg.Any<Blob>());
    }

    [Fact]
    public async Task HandleAsync_ShouldUseFileNameFromRequest()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var fileName = "my-custom-image-name.png";
        var contentType = "image/png";
        var mockFile = CreateMockFile(fileName, contentType);
        var request = new UploadBlobRequest(mockFile, userId);

        var expectedBlob = new Blob
        {
            Id = Guid.NewGuid(),
            Name = fileName,
            Path = $"files/{fileName}",
            UserId = userId
        };

        _mockBlobService.UploadBlob(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedBlob));

        // Act
        await _handler.HandleAsync(request);

        // Assert
        await _mockBlobService.Received(1).UploadBlob(
            Arg.Any<string>(),
            fileName,
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    private static IFile CreateMockFile(string fileName, string contentType)
    {
        var mockFile = Substitute.For<IFile>();
        mockFile.Name.Returns(fileName);
        mockFile.ContentType.Returns(contentType);
        mockFile.OpenReadStream().Returns(new MemoryStream([0x89, 0x50, 0x4E, 0x47])); // PNG header bytes
        return mockFile;
    }
}

