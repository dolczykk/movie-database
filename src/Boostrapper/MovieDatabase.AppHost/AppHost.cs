using Microsoft.Extensions.Hosting;

using MovieDatabase.SharedKernel.Configurations;

var builder = DistributedApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();
var cosmos = builder.AddAzureCosmosDB(CosmosConfiguration.ModuleName);

if (isDevelopment)
{
    cosmos.RunAsEmulator();
}

cosmos.AddCosmosDatabase(CosmosConfiguration.DbResourceName, CosmosConfiguration.DbName);

builder.AddProject<Projects.MovieDatabase_Api>(ApiConfiguration.ModuleName)
    .WithReference(cosmos)
    .WaitFor(cosmos);

builder.Build().Run();

public partial class Program;
