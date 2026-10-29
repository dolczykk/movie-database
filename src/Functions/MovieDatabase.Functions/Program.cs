using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MovieDatabase.SharedKernel.Configurations;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.AddAzureBlobServiceClient(BlobStorageConfiguration.ContainerName);
builder.AddServiceDefaults();

var app = builder.Build();

// ImageBlobTrigger's [BlobOutput] binding auto-creates the "content"
// container on its first write, with no way for the binding itself to
// request public access -- left to its default, the container ends up
// private, so the browser's anonymous <img> request for a thumbnail fails
// with AuthorizationFailure. Ensuring public access here, once at startup,
// mirrors what BlobService.UploadBlob already does explicitly for the API's
// "images" container.
//
// Both calls are needed: CreateIfNotExistsAsync's access-type argument only
// takes effect when it actually creates the container, so on a container
// that already exists with the wrong (private) access level -- e.g. the
// Azurite dev volume, which is persistent across AppHost restarts -- it is a
// no-op. SetAccessPolicyAsync is what corrects an already-existing
// container; it requires the container to exist first, hence both calls.
using (var scope = app.Services.CreateScope())
{
    var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();
    var contentContainer = blobServiceClient.GetBlobContainerClient(BlobStorageConfiguration.ContentContainerName);
    await contentContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
    await contentContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
}

app.Run();
