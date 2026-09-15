# Movie Database

A GraphQL-based movie database API built with .NET 9, HotChocolate, and Azure Cosmos DB. This project demonstrates modern .NET development practices including integration testing with real infrastructure via .NET Aspire.

## Running the Application

The application uses .NET Aspire for orchestration and runs the API with Azure Cosmos DB emulator in development.

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required for Cosmos DB emulator)
- Windows, macOS, or Linux

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd movie-database
   ```

2. **Run the AppHost project**
   ```bash
   cd src\Boostrapper\MovieDatabase.AppHost
   dotnet run
   ```

   Alternatively, from the solution root:
   ```bash
   dotnet run --project src\Boostrapper\MovieDatabase.AppHost\MovieDatabase.AppHost.csproj
   ```

3. **Access the Aspire Dashboard**
   
   The Aspire dashboard will launch automatically (usually at `http://localhost:15000` or similar). The terminal will display the exact URL. From the dashboard you can:
   - Monitor all running services
   - View logs in real-time
   - See metrics and traces
   - Access the API endpoint URLs

4. **Access the GraphQL endpoint**
   
   Once the API service is running, navigate to the API URL shown in the Aspire dashboard (typically `https://localhost:7xxx/graphql` or `http://localhost:5xxx/graphql`). This opens the Banana Cake Pop GraphQL IDE where you can:
   - Explore the schema
   - Run queries and mutations
   - Test authentication flows

### First Run

On the first run, Docker will download the Azure Cosmos DB emulator image (~2GB). This takes a few minutes but only happens once. Aspire will:
1. Start the Cosmos DB emulator container
2. Create the database and container
3. Seed initial data (including an admin user: `admin@example.com` / `test`)
4. Start the API service

### Running Tests

```bash
cd test\MovieDatabase.IntegrationTests
dotnet test
```

The first test run will also download the Cosmos DB emulator if you haven't run the application yet. Subsequent runs are faster since the emulator container is cached.

### Frontend

The Nuxt frontend lives in `src/Frontend/MovieDatabase.Web` and starts automatically with the
AppHost — open the `web` resource from the Aspire dashboard. The AppHost injects the API's URL
into the Nuxt process as `NUXT_GRAPHQL_ENDPOINT` (via `WithReference`/`WithEnvironment` on the
`web` resource in `AppHost.cs`), so no manual configuration is needed in this mode.

To run it standalone against an already-running API:

```bash
cd src/Frontend/MovieDatabase.Web
cp .env.example .env    # then set NUXT_GRAPHQL_ENDPOINT to the API's URL
pnpm install
pnpm dev
```

Regenerate the typed GraphQL client after any API schema change:

```bash
dotnet run --project utils/MovieDatabase.GenerateSchema/MovieDatabase.GenerateSchema.csproj
cd src/Frontend/MovieDatabase.Web && pnpm codegen
```

**A note for dependency upgrades**: `typescript` is constrained to the 5.x line (`^5.9.3`) in
`src/Frontend/MovieDatabase.Web/package.json`. TypeScript 7.0.2 crashes `vue-tsc` — TS7's package
exports dropped the classic `typescript/lib/tsc` entry point that `vue-tsc` (and therefore
`pnpm typecheck` / `nuxt typecheck`) depends on. Do not let Renovate/Dependabot bump `typescript`
past the `5.x` line until `vue-tsc` supports TypeScript 7's new export surface.

### Troubleshooting

**Docker not running**: Ensure Docker Desktop is started before running the application.

**Port conflicts**: If default ports are in use, Aspire will automatically assign different ports. Check the dashboard URL in the terminal output.

**Emulator issues**: If the Cosmos DB emulator fails to start, try:
```bash
docker ps -a
docker rm <cosmos-container-id>
dotnet run --project src\Boostrapper\MovieDatabase.AppHost\MovieDatabase.AppHost.csproj
```

## Integration Tests

Comprehensive integration tests cover the full GraphQL API including authentication, authorization, and CRUD operations. See [`test/MovieDatabase.IntegrationTests/`](test/MovieDatabase.IntegrationTests/) for details.

### Techniques

**[Aspire Distributed Application Testing](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/testing)**  
Uses the `DistributedApplicationTestingBuilder` pattern to spin up the entire application stack (API + Cosmos DB emulator) for each test run. This provides true integration testing against real infrastructure without manual setup.

**[xUnit Class Fixtures](https://xunit.net/docs/shared-context)**  
Implements `IClassFixture<AspireAppHostFixture>` to share expensive setup/teardown across test classes. The Aspire host starts once per test class rather than per test method, significantly improving test execution time.

**[IAsyncLifetime](https://xunit.net/docs/shared-context#async-lifetime)**  
The `AspireAppHostFixture` implements this interface to handle asynchronous initialization and cleanup of the distributed application, ensuring the test infrastructure is ready before tests execute.

**GraphQL Client Pattern**  
Tests use raw HTTP POST requests with JSON payloads rather than a typed GraphQL client. This approach validates the actual HTTP contract and makes tests framework-agnostic. See [`test/MovieDatabase.IntegrationTests/Helpers/GraphQLHelper.cs`](test/MovieDatabase.IntegrationTests/Helpers/GraphQLHelper.cs) for the implementation.

**Generic Response Wrapper**  
A custom `GraphQLResponse<T>` type provides strongly-typed access to GraphQL responses while preserving error information. This pattern allows type-safe assertions without losing access to GraphQL-specific error details.

**Bearer Token Authentication**  
Tests authenticate by adding JWT tokens to the `Authorization` header using [`AuthenticationHeaderValue`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.headers.authenticationheadervalue). Helper methods in [`test/MovieDatabase.IntegrationTests/Helpers/AuthenticationHelper.cs`](test/MovieDatabase.IntegrationTests/Helpers/AuthenticationHelper.cs) handle user registration and admin login flows.

**Dynamic Test Data Generation**  
Uses [`Guid.NewGuid()`](https://learn.microsoft.com/en-us/dotnet/api/system.guid.newguid) with the `:N` format specifier to generate unique usernames and emails for each test run, preventing conflicts and enabling parallel test execution.

**Flexible Error Assertions**  
Rather than brittle exact-match assertions, tests check for multiple possible error indicators (status codes, error message patterns) to handle variations in how different layers report failures.

## Technologies

**[.NET 9](https://dotnet.microsoft.com/)**  
The latest LTS release of .NET, featuring improved performance and C# 12 language features like collection expressions and primary constructors.

**[.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)**  
Microsoft's opinionated stack for building cloud-native distributed applications. Provides orchestration, service discovery, and testing infrastructure. Uses the `Aspire.Hosting.Testing` package (v9.5.1) to enable integration testing against the full application topology.

**[xUnit](https://xunit.net/)** (v2.9.2)  
The test framework. Chosen for its support of async fixtures, collection fixtures, and excellent integration with .NET tooling.

**[HotChocolate GraphQL Server](https://chillicream.com/docs/hotchocolate/v13)**  
A GraphQL server implementation for .NET. The API uses this to expose queries and mutations. Tests interact with the `/graphql` endpoint directly.

**[Azure Cosmos DB](https://learn.microsoft.com/en-us/azure/cosmos-db/)**  
NoSQL database for storing movie data. In development, the application uses the [Cosmos DB emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator) which runs in Docker. Tests run against the emulator to validate actual database integration including serialization, query performance, and partitioning behavior.

**[System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)**  
Used for JSON serialization/deserialization of GraphQL requests and responses. Configured with `PropertyNameCaseInsensitive = true` to handle GraphQL's camelCase conventions.

**[HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)**  
All API communication happens through `HttpClient` instances created by the Aspire test host, ensuring requests route through the same networking stack as production.

## Project Structure

```
movie-database/
├── src/
│   ├── Api/
│   │   ├── MovieDatabase.Api/
│   │   ├── MovieDatabase.Api.Application/
│   │   ├── MovieDatabase.Api.Core/
│   │   └── MovieDatabase.Api.Infrastructure/
│   └── Boostrapper/
│       ├── MovieDatabase.AppHost/
│       └── MovieDatabase.ServiceDefaults/
└── test/
    └── MovieDatabase.IntegrationTests/
        ├── Helpers/
        ├── Mutations/
        ├── Queries/
        └── Workflows/
```

**[`src/Api/MovieDatabase.Api/`](src/Api/MovieDatabase.Api/)** - Main API project. Contains the GraphQL schema definition in [`Query.cs`](src/Api/MovieDatabase.Api/Query.cs) and [`Program.cs`](src/Api/MovieDatabase.Api/Program.cs) which configures HotChocolate, authentication, and Cosmos DB.

**[`src/Api/MovieDatabase.Api.Application/`](src/Api/MovieDatabase.Api.Application/)** - Application layer containing business logic for directors, films, and users. Implements CQRS command/query handlers.

**[`src/Api/MovieDatabase.Api.Core/`](src/Api/MovieDatabase.Api.Core/)** - Core domain layer with CQRS interfaces, DTOs, domain documents, JWT services, and custom exceptions.

**[`src/Api/MovieDatabase.Api.Infrastructure/`](src/Api/MovieDatabase.Api.Infrastructure/)** - Infrastructure layer handling database access, Cosmos DB initialization, and data seeding.

**[`src/Boostrapper/MovieDatabase.AppHost/`](src/Boostrapper/MovieDatabase.AppHost/)** - .NET Aspire orchestration project. [`AppHost.cs`](src/Boostrapper/MovieDatabase.AppHost/AppHost.cs) configures the Cosmos DB emulator and API service for development and testing.

**[`src/Boostrapper/MovieDatabase.ServiceDefaults/`](src/Boostrapper/MovieDatabase.ServiceDefaults/)** - Shared service configuration and extensions used across Aspire-managed services.

**[`test/MovieDatabase.IntegrationTests/Helpers/`](test/MovieDatabase.IntegrationTests/Helpers/)** - Shared utilities for GraphQL execution, authentication flows, and response parsing. The `GraphQLHelper` class provides methods for executing queries/mutations and deserializing responses. The `AuthenticationHelper` handles user registration and login.

**[`test/MovieDatabase.IntegrationTests/Mutations/`](test/MovieDatabase.IntegrationTests/Mutations/)** - Tests for GraphQL mutations (create/update/delete operations). Includes `FilmMutationTests` (CRUD operations with admin/moderator roles) and `UserMutationTests` (registration, login, duplicate email validation).

**[`test/MovieDatabase.IntegrationTests/Queries/`](test/MovieDatabase.IntegrationTests/Queries/)** - Tests for GraphQL queries (read operations). Covers all five entity types: films, actors, directors, genres, and producers. Each test file validates both unfiltered listing and search/filter scenarios.

**[`test/MovieDatabase.IntegrationTests/Workflows/`](test/MovieDatabase.IntegrationTests/Workflows/)** - End-to-end workflow tests that combine multiple operations. Reserved for testing complete user journeys across multiple API calls.

## Test Coverage

The integration test suite includes 19+ tests covering:

- **Query Operations**: All five entity types (films, actors, directors, genres, producers) with filtering
- **Nested Data**: Tests verify that GraphQL properly loads related entities (films include actors, directors, genres, producers)
- **Authentication**: User registration, login, duplicate email validation, invalid credentials
- **Authorization**: Tests confirm unauthorized requests fail and role-based access works (admin, moderator)
- **CRUD Operations**: Create, read, update, and delete films with proper authorization
- **Error Handling**: Validates error responses for both application-level and HTTP-level failures

