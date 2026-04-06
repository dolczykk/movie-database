using MovieDatabase.Api;
using MovieDatabase.Api.Application;
using MovieDatabase.Api.Core;
using MovieDatabase.Api.Infrastructure;
using MovieDatabase.Api.Infrastructure.Db;
using MovieDatabase.Api.Mutations;
using MovieDatabase.SharedKernel.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddCosmosDbContext<AppDbContext>(CosmosConfiguration.ModuleName, databaseName: CosmosConfiguration.DbName, configureDbContextOptions:
    optionsBuilder =>
    {
        optionsBuilder.UseAsyncSeeding(async (seed, created, ct) =>
        {
            if (!created)
            {
                return;
            }

            await DbSeeder.SeedUsers(seed, ct);
            await DbSeeder.SeedFilms(seed, ct);

            await seed.SaveChangesAsync(ct);
        });
    });

builder.AddAzureBlobServiceClient(BlobStorageConfiguration.ContainerName);

builder.Services.AddApplicationDefaults();
builder.Services.AddInfrastructureDefaults(builder.Configuration);
builder.Services.AddCoreDefaults(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddType<UploadType>()
    .RegisterDbContextFactory<AppDbContext>()
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
    .AddQueryType<Query>();


var app = builder.Build();

using var scope = app.Services.CreateScope();

var applicationDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await applicationDbContext.Database.EnsureCreatedAsync();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();

public partial class Program;