using Microsoft.Extensions.Hosting;

using MovieDatabase.SharedKernel.Configurations;

var builder = DistributedApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();
var cosmos = builder.AddAzureCosmosDB(CosmosConfiguration.ModuleName);

var storage = builder.AddAzureStorage(BlobStorageConfiguration.ModuleName);
var blobs = storage.AddBlobs(BlobStorageConfiguration.ContainerName);


if (isDevelopment)
{
    cosmos.RunAsEmulator();
    storage.RunAsEmulator(azurite =>
    {
        azurite.WithLifetime(ContainerLifetime.Persistent);
    });
}

cosmos.AddCosmosDatabase(CosmosConfiguration.DbResourceName, CosmosConfiguration.DbName);

var functions = builder.AddAzureFunctionsProject<Projects.MovieDatabase_Functions>("functions")
    .WithHostStorage(storage)
    .WithReference(blobs);

builder.AddProject<Projects.MovieDatabase_Api>(ApiConfiguration.ModuleName)
    .WithReference(cosmos)
    .WithReference(blobs)
    .WithReference(functions)
    .WaitFor(blobs)
    .WaitFor(cosmos);

builder.Build().Run();

public partial class Program;