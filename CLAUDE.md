# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A GraphQL movie database API built on .NET 10, HotChocolate, EF Core over Azure Cosmos DB, orchestrated with .NET Aspire. A companion Azure Functions app generates image thumbnails from uploaded blobs.

## Commands

Run everything from the repository root unless noted.

```bash
# Build the whole solution
dotnet build MovieDatabase.sln

# Run the app (starts Aspire dashboard, Cosmos DB emulator, Azurite, API, and Functions)
dotnet run --project src/Boostrapper/MovieDatabase.AppHost/MovieDatabase.AppHost.csproj
# Docker Desktop must be running (Cosmos DB emulator + Azurite run as containers).

# Unit tests (fast, no external dependencies)
dotnet test test/MovieDatabase.UnitTests/MovieDatabase.UnitTests.csproj

# Run a single unit test
dotnet test test/MovieDatabase.UnitTests/MovieDatabase.UnitTests.csproj --filter "FullyQualifiedName~CreateFilmRequestHandlerTests"

# Integration tests (spins up the full Aspire distributed app per test class, incl. Cosmos DB emulator)
dotnet test test/MovieDatabase.IntegrationTests/MovieDatabase.IntegrationTests.csproj
```

CI (`.github/workflows/test.yml`) runs on .NET SDK `10.0.201`, does `dotnet restore` / `dotnet build --no-restore` / `dotnet test --no-restore` for the whole solution, and trusts the dev HTTPS cert first — integration tests need that cert trusted locally too.

## Architecture

### Layering (`src/Api/`)

Requests flow `MovieDatabase.Api` (HotChocolate GraphQL layer) → `MovieDatabase.Api.Application` (CQRS handlers) → `MovieDatabase.Api.Infrastructure` (EF Core repositories over Cosmos DB), all depending on `MovieDatabase.Api.Core` (domain documents, DTOs, CQRS contracts, JWT/exception/util types).

- **Queries** are plain HotChocolate resolvers directly on `AppDbContext` (`src/Api/MovieDatabase.Api/Query.cs`) — no CQRS indirection for reads, `[UsePaging] [UseFiltering] [UseSorting]` on `IQueryable<T>`.
- **Mutations** go through a custom CQRS dispatcher: a `[ExtendObjectType("Mutation")]` class (e.g. `FilmMutations`) builds an `IRequest<TResponse>` and calls `IDispatcher.Dispatch(request)`. `Dispatcher` (`Core/Cqrs/Dispatcher.cs`) resolves `IRequestHandler<TRequest, TResponse>` from DI via reflection + `dynamic` dispatch and throws `RequestHandlerNotFoundException` if nothing is registered.
- New CQRS features need a handler registered explicitly in `ApplicationExtensions.RegisterHandlers()` — there's no auto-discovery/assembly scanning.
- Each request feature lives in its own folder under `Application/<Area>/<Verb><Entity>/` with `*Request.cs` (+`*Input.cs` for GraphQL input types) and `*RequestHandler.cs` side by side.
- `Api`, `Application`, `Infrastructure`, and `Core` each expose a single `Add*Defaults(...)` extension method (`AddApplicationDefaults`, `AddInfrastructureDefaults`, `AddCoreDefaults`) called from `Program.cs` — new services/repositories/handlers are registered inside these, not ad hoc in `Program.cs`.

### Data layer

- `AppDbContext` (`Api.Infrastructure/Db/AppDbContext.cs`) maps `Film`, `User`, `Blob` to separate Cosmos containers via `ToContainer(...)` + `HasPartitionKey(...)`. `Film` owns `Director`/`Producer` (`OwnsOne`) and `Actors`/`Genres` (`OwnsMany`) as embedded sub-documents in the same container — there is no relational join.
- Soft delete: all three entities have `HasQueryFilter(x => !x.IsDeleted)` — deletes should set `IsDeleted`, not remove the Cosmos item.
- Repository + `IUnitOfWork` pattern wraps `AppDbContext`; handlers call `repository.Add/...` then `unitOfWork.Commit()` (`SaveChangesAsync` under the hood).

### Auth

JWT bearer auth is configured in `Infrastructure/InfrastructureExtensions.cs` (`Jwt:Issuer`/`Jwt:Audience`/`Jwt:Key` from configuration, `RoleClaimType = ClaimTypes.Role`). Mutations authorize with HotChocolate's `[Authorize(Roles = [nameof(UserRoles.Administrator)])]`; the acting user id is read from the JWT `Jti` claim inside the mutation resolver and threaded onto the CQRS request (`request.CreatorId` / `request.UserId`) rather than resolved inside the handler.

### Aspire orchestration (`src/Boostrapper/`)

`AppHost.cs` wires: Azure Cosmos DB (emulator in dev via `RunAsPreviewEmulator()` — the Linux/vNext-preview emulator image, not the classic `RunAsEmulator()`), Azure Storage/Blobs (Azurite emulator in dev, `ContainerLifetime.Persistent`), the `MovieDatabase.Api` project, and the `MovieDatabase.Functions` Azure Functions project (`WithHostStorage`, referencing the blob container). Resource/module names are centralized as constants in `src/Shared/MovieDatabase.SharedKernel/Configurations/*Configuration.cs` (`CosmosConfiguration`, `BlobStorageConfiguration`, `ApiConfiguration`) — reuse these instead of hardcoding resource-name strings. Unlike the Azurite container, the Cosmos emulator container has no persistent volume — every AppHost restart reseeds a fresh, empty database.

### Functions (`src/Functions/MovieDatabase.Functions/`)

Isolated-worker Azure Functions app. `ImageBlobTrigger` fires on blobs uploaded to the `images/{baseName}.{ext}` path, re-encodes them as JPEG via ImageSharp, and writes the thumbnail to `thumbnails/{baseName}.jpg` (`[BlobOutput]`).

### Testing

- **Unit tests** (`test/MovieDatabase.UnitTests`) target CQRS handlers and Core services/utils directly, using NSubstitute for mocking repositories/services and AutoFixture (`AutoFixture.Xunit2`) for test data generation; assertions use Shouldly.
- **Integration tests** (`test/MovieDatabase.IntegrationTests`) use `Aspire.Hosting.Testing`'s `DistributedApplicationTestingBuilder` to boot the real AppHost topology (API + Cosmos DB emulator) per test class via `IClassFixture<AspireAppHostFixture>` (`Fixtures/AspireAppHostFixture.cs`, `IAsyncLifetime`). Tests POST raw JSON to `/graphql` through `GraphQLHelper` rather than using a typed GraphQL client, deserializing into a generic `GraphQLResponse<T>`. `AuthenticationHelper` handles registration/login to obtain bearer tokens for authorized mutation tests. Test data uses `Guid.NewGuid("N")` for unique emails/usernames to allow parallel execution.
- Seed users and film data are loaded via `DbSeeder` on first Cosmos DB creation (`UseAsyncSeeding` in `Program.cs`), reading `src/Api/MovieDatabase.Api/Data/users.json` / `films.json`. These are Bogus/Faker-generated fixtures (`utils/MovieDatabase.FakeDataGenerator`), not fixed `admin@example.com`-style accounts — every seeded user shares the plaintext password `example123!` (`BogusUserExtensions.cs`), but emails/roles are random per generation, so check `Data/users.json` for the current `Administrator`-role entry's email rather than assuming one.

### Utils (`utils/`)

`MovieDatabase.GenerateSchema` and `MovieDatabase.FakeDataGenerator` are standalone dev-time tools (schema export, generating fake seed data) — not part of the runtime app graph.
