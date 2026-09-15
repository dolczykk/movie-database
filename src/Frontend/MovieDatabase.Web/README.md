# MovieDatabase.Web

Nuxt 4 frontend for the Movie Database GraphQL API. See the [repository
root README](../../../README.md) for the full picture (running the whole
stack under Aspire, seeded admin login, etc). This file only covers the
frontend project itself.

**pnpm only** — this project pins its package manager (`packageManager` in
`package.json`); do not use `npm` or `yarn`.

## Running under Aspire (normal case)

Start the AppHost from the repo root:

```bash
dotnet run --project src/Boostrapper/MovieDatabase.AppHost/MovieDatabase.AppHost.csproj
```

The `web` resource starts automatically (open it from the Aspire dashboard).
The AppHost injects the API's URL as `NUXT_GRAPHQL_ENDPOINT` — no manual
configuration needed in this mode.

## Running standalone

Against an already-running API:

```bash
cp .env.example .env    # then set NUXT_GRAPHQL_ENDPOINT to the API's URL
pnpm install
pnpm dev
```

## Other commands

```bash
pnpm build       # production build
pnpm test        # vitest
pnpm typecheck   # nuxt typecheck (vue-tsc)
pnpm codegen     # regenerate graphql/generated/ from the API schema
```

Regenerate the typed GraphQL client after any API schema change:

```bash
dotnet run --project utils/MovieDatabase.GenerateSchema/MovieDatabase.GenerateSchema.csproj
pnpm codegen
```
