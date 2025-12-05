using HotChocolate.Execution;

using Microsoft.Extensions.DependencyInjection;

using MovieDatabase.Api;
using MovieDatabase.Api.Mutations;

var serviceCollection = new ServiceCollection();

serviceCollection.AddGraphQL()
    .AddQueryType<Query>()
    .AddAuthorization()
    .AddMutationType(d => d.Name("Mutation"))
    .AddFiltering()
    .AddSorting()
    .AddPagingArguments()
    .AddTypeExtension<FilmMutations>()
    .AddTypeExtension<UserMutations>();

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