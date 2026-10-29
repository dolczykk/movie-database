using Azure.Storage.Blobs;

using MovieDatabase.Api.Core.Services;

using Shouldly;

namespace MovieDatabase.UnitTests.Core.Services;

public class BlobServiceTests
{
    // Azurite's well-known default connection string (same account name/key
    // every Azurite instance uses). `BlobServiceClient` resolves the URI
    // locally from this -- no network call -- so this exercises the real
    // Azure SDK's URI shape without needing a running emulator.
    private const string AzuriteConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    [Fact]
    public void GetContentUrl_WithAzuriteStyleEndpoint_IncludesTheAccountPathSegment()
    {
        // Azurite serves path-style URLs (http://host:port/{account}/{container}/{blob}),
        // unlike production Azure Blob Storage, which puts the account in the
        // hostname. `BlobServiceClient.Uri` for Azurite already carries that
        // account segment in its path -- a content URL built from only the
        // URI's authority (scheme://host:port) silently drops it.
        var blobServiceClient = new BlobServiceClient(AzuriteConnectionString);
        var sut = new BlobService(blobServiceClient);

        var url = sut.GetContentUrl("9506f0c9-be22-4bf0-9922-62b170c2f567");

        url.ShouldBe("http://127.0.0.1:10000/devstoreaccount1/content/9506f0c9-be22-4bf0-9922-62b170c2f567.jpg");
    }

    [Fact]
    public void GetContentUrl_WithProductionAzureEndpoint_HasNoExtraPathSegment()
    {
        // Production Azure Blob Storage puts the account in the hostname
        // (https://{account}.blob.core.windows.net), so there is no account
        // path segment to carry through here -- this is the case the
        // original (buggy) implementation happened to get right by accident.
        var blobServiceClient = new BlobServiceClient(new Uri("https://myaccount.blob.core.windows.net"));
        var sut = new BlobService(blobServiceClient);

        var url = sut.GetContentUrl("9506f0c9-be22-4bf0-9922-62b170c2f567");

        url.ShouldBe("https://myaccount.blob.core.windows.net/content/9506f0c9-be22-4bf0-9922-62b170c2f567.jpg");
    }
}
