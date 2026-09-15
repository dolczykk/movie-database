using HotChocolate.Execution;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using MovieDatabase.Api;
using MovieDatabase.Api.Core;
using MovieDatabase.Api.Mutations;
using MovieDatabase.Api.Types;

var serviceCollection = new ServiceCollection();

serviceCollection.AddGraphQL()
    .AddAuthorization()
    .AddType<UploadType>()
    .AddMutationType(d => d.Name("Mutation"))
    .AddFiltering()
    .AddSorting()
    .AddPagingArguments()
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = Constants.Query.MaxPageSize;
        opt.DefaultPageSize = Constants.Query.DefaultPageSize;
    })
    .AddTypeExtension<FilmMutations>()
    .AddTypeExtension<UserMutations>()
    .AddTypeExtension<TokenMutations>()
    .AddTypeExtension<BlobMutations>()
    .AddTypeExtension<FilmTypeExtensions>()
    .AddQueryType<Query>();

await using var services = serviceCollection.BuildServiceProvider();
var executor = await services.GetRequiredService<IRequestExecutorResolver>()
    .GetRequestExecutorAsync();

var schema = executor.Schema;
var sdl = schema.Print();

var outputPath = Path.Combine(
    Environment.CurrentDirectory,
    "test/MovieDatabase.IntegrationTests/schema.graphql");

File.WriteAllText(outputPath, sdl);

Console.WriteLine($"Generated schema at {outputPath}");
