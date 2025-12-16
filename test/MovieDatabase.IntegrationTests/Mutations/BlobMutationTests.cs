using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using MovieDatabase.Api.Application.Users.AuthenticateUser;
using MovieDatabase.IntegrationTests.Fixtures;
using MovieDatabase.IntegrationTests.Helpers;
using MovieDatabase.IntegrationTests.Responses.Users;

using Shouldly;

namespace MovieDatabase.IntegrationTests.Mutations;

[Collection("AspireAppHost")]
public class BlobMutationTests(AspireAppHostFixture fixture)
{
    private readonly HttpClient _httpClient = fixture.CreateHttpClient("movies-db-api");

    [Fact]
    public async Task UploadBlob_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var fileContent = CreateTestImageBytes();
        var fileName = "test-image.png";
        var contentType = "image/png";

        // Act
        var response = await ExecuteUploadBlobMutationRaw(_httpClient, fileContent, fileName, contentType);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var hasAuthError = content.Contains("authorize", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("authenticated", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("AUTH_NOT_AUTHENTICATED", StringComparison.OrdinalIgnoreCase) ||
                          response.StatusCode == HttpStatusCode.Unauthorized;
        hasAuthError.ShouldBeTrue($"Expected authorization error but got: {content}");
    }

    [Fact]
    public async Task UploadBlob_WithValidPngImage_ShouldUploadSuccessfully()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var client = fixture.CreateHttpClient("movies-db-api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fileContent = CreateTestImageBytes();
        var fileName = $"test-image-{Guid.NewGuid():N}.png";
        var contentType = "image/png";

        // Act
        var response = await ExecuteUploadBlobMutation(client, fileContent, fileName, contentType);

        // Assert
        response.ShouldNotBeNull();
        var errorMessage = response.Errors != null ? string.Join(", ", response.Errors.Select(e => e.Message)) : null;
        response.Errors.ShouldBeNull($"Expected no errors but got: {errorMessage}");
        response.Data.ShouldNotBeNull();
        response.Data.UploadBlob.ShouldNotBeNull();
        response.Data.UploadBlob.Id.ShouldNotBeNullOrEmpty();
        response.Data.UploadBlob.FileName.ShouldNotBeNullOrEmpty();
        response.Data.UploadBlob.Url.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadBlob_WithValidJpegImage_ShouldUploadSuccessfully()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var client = fixture.CreateHttpClient("movies-db-api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fileContent = CreateTestJpegBytes();
        var fileName = $"test-image-{Guid.NewGuid():N}.jpeg";
        var contentType = "image/jpeg";

        // Act
        var response = await ExecuteUploadBlobMutation(client, fileContent, fileName, contentType);

        // Assert
        response.ShouldNotBeNull();
        var errorMessage = response.Errors != null ? string.Join(", ", response.Errors.Select(e => e.Message)) : null;
        response.Errors.ShouldBeNull($"Expected no errors but got: {errorMessage}");
        response.Data.ShouldNotBeNull();
        response.Data.UploadBlob.ShouldNotBeNull();
        response.Data.UploadBlob.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadBlob_WithValidWebpImage_ShouldUploadSuccessfully()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var client = fixture.CreateHttpClient("movies-db-api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fileContent = CreateTestWebpBytes();
        var fileName = $"test-image-{Guid.NewGuid():N}.webp";
        var contentType = "image/webp";

        // Act
        var response = await ExecuteUploadBlobMutation(client, fileContent, fileName, contentType);

        // Assert
        response.ShouldNotBeNull();
        var errorMessage = response.Errors != null ? string.Join(", ", response.Errors.Select(e => e.Message)) : null;
        response.Errors.ShouldBeNull($"Expected no errors but got: {errorMessage}");
        response.Data.ShouldNotBeNull();
        response.Data.UploadBlob.ShouldNotBeNull();
        response.Data.UploadBlob.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UploadBlob_WithUnsupportedContentType_ShouldReturnError()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var client = fixture.CreateHttpClient("movies-db-api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fileContent = Encoding.UTF8.GetBytes("This is a text file");
        var fileName = $"test-file-{Guid.NewGuid():N}.txt";
        var contentType = "text/plain";

        // Act
        var response = await ExecuteUploadBlobMutationRaw(client, fileContent, fileName, contentType);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var hasError = content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("not supported", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("NotSupportedContentType", StringComparison.OrdinalIgnoreCase);
        hasError.ShouldBeTrue($"Expected content type error but got: {content}");
    }

    [Fact]
    public async Task UploadBlob_WithPdfFile_ShouldReturnError()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var client = fixture.CreateHttpClient("movies-db-api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fileContent = CreateTestPdfBytes();
        var fileName = $"test-file-{Guid.NewGuid():N}.pdf";
        var contentType = "application/pdf";

        // Act
        var response = await ExecuteUploadBlobMutationRaw(client, fileContent, fileName, contentType);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var hasError = content.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("not supported", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("NotSupportedContentType", StringComparison.OrdinalIgnoreCase);
        hasError.ShouldBeTrue($"Expected content type error but got: {content}");
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginMutation = GraphQLHelper.LoadQueryFromFile("Graphql/Mutations/LoginUser.graphql");

        var request = new AuthenticateUserRequest(
            Email: "Favian74@example.net",
            Password: "example123!"
        );

        var loginResponse = await GraphQLHelper.ExecuteMutationAsync<LoginUserResponse>(
            _httpClient, loginMutation, new { request });

        if (loginResponse is { Data.LoginUser.Token: { } token })
        {
            return token;
        }

        var error = loginResponse?.Errors?.FirstOrDefault();
        throw new Exception($"Could not get admin token. Error: {error?.Message ?? "Unknown error"}. Check if seeded admin exists with email 'Favian74@example.net' and password 'example123!'");
    }

    private static async Task<GraphQLResponse<UploadBlobResponseData>?> ExecuteUploadBlobMutation(
        HttpClient client,
        byte[] fileContent,
        string fileName,
        string contentType)
    {
        var response = await ExecuteUploadBlobMutationRaw(client, fileContent, fileName, contentType);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<GraphQLResponse<UploadBlobResponseData>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // If deserialization failed to capture errors, try to include raw response
        if (result?.Errors == null && content.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"GraphQL request may have failed. Raw response: {content}");
        }

        return result;
    }

    private static async Task<HttpResponseMessage> ExecuteUploadBlobMutationRaw(
        HttpClient client,
        byte[] fileContent,
        string fileName,
        string contentType)
    {
        // GraphQL multipart request specification
        // https://github.com/jaydenseric/graphql-multipart-request-spec
        
        const string mutation = """
            mutation UploadBlob($file: Upload!) {
              uploadBlob(file: $file) {
                id
                fileName
                url
              }
            }
            """;

        var operations = new
        {
            query = mutation,
            variables = new { file = (object?)null }
        };

        var map = new Dictionary<string, string[]>
        {
            ["0"] = ["variables.file"]
        };

        using var formContent = new MultipartFormDataContent();
        
        formContent.Add(new StringContent(JsonSerializer.Serialize(operations), Encoding.UTF8, "application/json"), "operations");
        formContent.Add(new StringContent(JsonSerializer.Serialize(map), Encoding.UTF8, "application/json"), "map");

        var fileContentData = new ByteArrayContent(fileContent);
        fileContentData.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        formContent.Add(fileContentData, "0", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = formContent;
        request.Headers.Add("GraphQL-Preflight", "1");
        
        // Copy Authorization header from client if present
        if (client.DefaultRequestHeaders.Authorization != null)
        {
            request.Headers.Authorization = client.DefaultRequestHeaders.Authorization;
        }

        return await client.SendAsync(request);
    }

    // Creates a minimal valid PNG file (1x1 transparent pixel)
    private static byte[] CreateTestImageBytes()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1 dimension
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, // bit depth, color type, etc.
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, // compressed data
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, // CRC
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, // IEND chunk
            0x42, 0x60, 0x82
        ];
    }

    // Creates a minimal JPEG header
    private static byte[] CreateTestJpegBytes()
    {
        return
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, // JPEG SOI and APP0 marker
            0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, // JFIF identifier
            0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, // DQT marker
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, // Quantization table
            0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C,
            0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D,
            0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20,
            0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27,
            0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34,
            0x32, 0xFF, 0xD9 // EOI marker
        ];
    }

    // Creates a minimal WebP header
    private static byte[] CreateTestWebpBytes()
    {
        return
        [
            0x52, 0x49, 0x46, 0x46, // RIFF
            0x24, 0x00, 0x00, 0x00, // File size (36 bytes)
            0x57, 0x45, 0x42, 0x50, // WEBP
            0x56, 0x50, 0x38, 0x4C, // VP8L
            0x17, 0x00, 0x00, 0x00, // Chunk size
            0x2F, 0x00, 0x00, 0x00, // Signature
            0x00, 0x00, 0x00, 0x00, // Image data
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00
        ];
    }

    // Creates a minimal PDF header
    private static byte[] CreateTestPdfBytes()
    {
        return "%PDF-1.4\n1 0 obj\n<<>>\nendobj\n%%EOF"u8.ToArray();
    }

    private record UploadBlobResponseData(UploadBlobData UploadBlob);
    private record UploadBlobData(string Id, string FileName, string Url);
}

