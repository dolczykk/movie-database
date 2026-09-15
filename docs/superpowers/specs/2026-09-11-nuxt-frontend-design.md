# Nuxt Frontend for MovieDatabase.Api — Design

**Date:** 2026-09-11
**Status:** Approved design, pending implementation plan

## Context

`MovieDatabase.Api` exposes a HotChocolate GraphQL API (films catalogue, JWT auth with
Administrator/Moderator/User roles, blob upload) but has no user interface. This design covers a
Nuxt frontend that consumes it, with routing, authentication, role-based authorization, and
NuxtUI as the component library.

The API is already orchestrated by .NET Aspire alongside a Cosmos DB emulator, Azurite, and an
Azure Functions image-compression worker. The frontend joins that same orchestration so the whole
stack starts with one command.

## Decisions

| Decision | Choice | Why |
|---|---|---|
| Rendering | SSR / universal | Film pages are public content; server-rendered HTML gives SEO and faster first paint. |
| GraphQL client | `@nuxtjs/apollo` + GraphQL Code Generator | Normalized cache and SSR support out of the box; codegen turns the API schema into TypeScript types so API drift breaks the build, not production. |
| Orchestration | Node resource in the Aspire AppHost | Matches how everything else in this repo runs; the API's URL arrives via service discovery instead of a hardcoded value. |
| Location / tooling | `src/Frontend/MovieDatabase.Web`, pnpm | Mirrors the existing `src/Api`, `src/Functions`, `src/Boostrapper` convention. |
| Token storage | HttpOnly cookies set by a Nuxt server proxy | Tokens unreadable by JS (XSS-safe) and available to SSR on the first request, so protected pages render correctly server-side. |

Versions (Nuxt major, NuxtUI major) are whatever `create nuxt@latest` scaffolds at implementation
time — pin them then rather than guessing here.

## Architecture

### Project layout

```
src/Frontend/MovieDatabase.Web/
├── app/
│   ├── components/          # ImageUploader, FilmCard, FilmForm, ...
│   ├── composables/         # useAuth, film queries/mutations
│   ├── middleware/          # auth, admin, moderator route guards
│   ├── pages/               # file-based routing
│   ├── stores/              # Pinia auth store
│   └── app.vue
├── server/
│   ├── api/
│   │   ├── auth/            # login, register, refresh, logout
│   │   ├── graphql.ts       # generic JSON proxy (browser → API)
│   │   └── upload.ts        # multipart proxy (browser → API)
│   └── utils/               # cookie + bearer-header helpers
├── graphql/                 # .graphql operation documents
├── codegen.ts
└── nuxt.config.ts
```

The project is **not** added to `MovieDatabase.sln` — it is not an MSBuild project. It is referenced
only from `AppHost.cs`.

### Authentication — backend-for-frontend proxy

HttpOnly cookies and a `Authorization: Bearer` API are not directly compatible: JavaScript cannot
read an HttpOnly cookie, so the browser can never construct the Bearer header itself. The API reads
JWTs only from the `Authorization` header (`AddJwtBearer` default), not from cookies. Therefore the
browser never calls the .NET API directly — it calls same-origin Nuxt server routes, which hold the
cookie and forward the Bearer header.

Both rendering contexts go through one generic proxy route, `server/api/graphql.post.ts`, which reads
the HttpOnly cookie, attaches the Bearer header, forwards the operation, and returns the GraphQL
envelope untouched:

- **Browser → Nuxt server → API:** a real same-origin request to the proxy.
- **SSR → (in-process) → API:** Nitro handles `$fetch` to its own route in-process with no network
  hop, so SSR uses the same path; it only has to forward the incoming request's `cookie` header for
  the proxy to read.

Keeping both contexts on one path means the cookie-to-Bearer translation lives in exactly one file,
and application code never imports from `server/` — a boundary Nuxt does not guarantee is safe to
cross. Queries and mutations are written once and work unchanged in both contexts.

**Consequence worth recording:** because the browser only ever makes same-origin requests, the .NET
API needs **no CORS configuration** (it currently has none). CORS is browser-enforced; server-to-server
SSR calls are unaffected by it.

Auth routes under `server/api/auth/`:

| Route | Calls | Effect |
|---|---|---|
| `login.post.ts` | `loginUser` mutation | Sets `accessToken` + `refreshToken` HttpOnly cookies; returns only `{ id, username, email, role }`. |
| `register.post.ts` | `registerUser` mutation | Same as login. |
| `refresh.post.ts` | `revoke` mutation | Exchanges the cookie pair for a fresh pair, rewrites cookies. Single-flight — see below. |
| `logout.post.ts` | — | Clears both cookies. |

Cookies: `HttpOnly`, `Secure`, `SameSite=Lax`, expiry matching the token's own expiry
(access 60 min, refresh 7 days, per `JwtSettings`).

A Nuxt plugin decodes the access token's payload locally (no network call) to populate a Pinia
`useAuthStore` with `{ user, role, isAuthenticated }` on both server and client boot.

#### Refresh must be proactive, and single-flight

Two properties of `RevokeTokenRequestHandler` constrain the refresh strategy, and both were verified
against the implementation rather than assumed:

1. **Refresh requires a still-valid access token.** The handler calls
   `IJwtService.ReadPrincipalFromExpiredToken(request.AccessToken)`, which — despite its name — sets
   `ValidateLifetime = true`. An expired access token throws, so refresh *cannot* recover an expired
   session. The frontend must refresh **before** expiry (e.g. when the token is within ~5 minutes of
   `exp`, checked on navigation and on a timer), not reactively after a 401. A session idle past the
   access token's 60-minute lifetime is unrecoverable and must redirect to login — the 7-day refresh
   token is effectively unusable on its own.
2. **Refresh tokens rotate and are single-use.** `FindUserToRevokeToken` matches only on
   `!IsRevoked && ExpiresAt > UtcNow`, and the handler marks the presented pair revoked before storing
   the new one. Two concurrent refreshes with the same pair means one succeeds and one hard-fails.
   The refresh call therefore needs **single-flight deduplication** — concurrent callers await one
   shared in-flight promise rather than each firing their own request.

#### Client-side role state is advisory, not a security boundary

`/api/auth/me` and the session endpoint decode the access token's payload locally — base64 only, **no
signature verification**. Verifying it would require the .NET API's `Jwt:Key` to also live in the
Nuxt server, putting the signing secret in a second place, and the API exposes no authenticated
endpoint a token could be probed against (every query is public; only mutations are gated).

The consequence, stated plainly: anyone can send a hand-crafted `Cookie: md_access_token=<forged
JWT>` header directly to `/api/auth/me` and receive a fabricated user with any role they like. What
that buys them is a user interface showing buttons they cannot use. Every privileged operation —
`createFilm` and `deleteFilm` (Administrator), `editFilm` (Administrator or Moderator), `uploadBlob`
(any authenticated user) — is enforced by the .NET API, which *does* validate signatures. Reads need
no authentication at all, so there is nothing to disclose.

**The route middleware in Task 12 is therefore UX, not authorization.** It decides which pages to
render, not what the user may do. Anyone extending this frontend must not move a real access-control
decision behind those guards.

**Revocation nuance:** the API *does* track issued tokens (`User.Tokens`, hashed, with an `IsRevoked`
flag), but `IsRevoked` is never consulted when authenticating a request — the JWT bearer pipeline does
pure stateless validation with no database lookup. Revocation therefore only prevents refresh-token
reuse; it does not invalidate an already-issued access token. Logout is consequently just "drop the
cookies", and the dropped access token remains technically valid until it expires. Not something this
frontend can fix.

### Route guards

Nuxt route middleware, applied via page `definePageMeta`:

- `auth` — any authenticated user (needed for `uploadBlob`).
- `moderator` — Administrator or Moderator (edit film).
- `admin` — Administrator only (create/delete film).

Unauthorized access redirects to `/login?redirect=<original path>`. Guards read from the Pinia store,
which is already populated server-side, so SSR renders the correct thing with no flash of wrong content.

Role values match the API's `UserRoles` enum exactly: `Administrator`, `Moderator`, `User`.

The claim is emitted as `new Claim(ClaimTypes.Role, ...)` in `JwtService.GenerateJwtToken`. Whether
that lands in the decoded payload under the short key `role` or the long
`http://schemas.microsoft.com/ws/2008/06/identity/claims/role` URI depends on
`JwtSecurityTokenHandler`'s outbound claim-type map in the installed version — **decode a real token
and confirm the key before writing the guard**, since a wrong key fails silently (every role check
just returns false).

### Data layer

GraphQL operations live as `.graphql` documents under `graphql/`. GraphQL Code Generator produces
TypeScript types and typed composables from them.

Codegen reads a local `schema.graphql` produced by the **existing** `utils/MovieDatabase.GenerateSchema`
tool, rather than requiring a running API for introspection. Regenerating the schema is a documented
step whenever the API changes.

### API surface consumed

Queries (all public — no `[Authorize]` on the query type):

- `films` — Relay cursor connection, supports `where` filtering and `order` sorting
  (`[UsePaging]`/`[UseFiltering]`/`[UseSorting]`). Page size: default 10, max 20.
- `actors`, `genres`, `directors`, `producers` — flattened connections over the same film documents.

The `Film` type exposes `thumbnailUrl` (a computed, ready-to-render URL to the compressed image in
the `content` container, resolved by `FilmTypeExtensions`) and currently also `thumbnail` (the raw
internal blob id). **The frontend uses `thumbnailUrl` and ignores `thumbnail`.** `thumbnailUrl` is
currently always `null` in practice — see Out of scope.

Mutations (argument names follow the C# parameter names):

| Mutation | Argument | Returns | Authorization |
|---|---|---|---|
| `registerUser` | `request: { username, email, password }` | `UserCredentialsDto` | anonymous |
| `loginUser` | `request: { email, password }` | `UserCredentialsDto` | anonymous |
| `revoke` | `input: { accessToken, refreshToken }` | `RevokeTokenDto` | anonymous |
| `createFilm` | `input: CreateFilmInput` | `FilmDto` | Administrator |
| `editFilm` | `input: EditFilmInput` | `FilmDto` | Administrator, Moderator |
| `deleteFilm` | `filmId: String!` | `String` | Administrator |
| `uploadBlob` | `file: Upload!` | `BlobDto` | any authenticated |

`UserCredentialsDto` carries `{ id, username, email, role, token, expireTime, refreshToken,
refreshTokenExpireTime }` — the server proxy strips the token fields before returning to the browser.

**`uploadBlob` needs its own proxy route.** It uses the GraphQL multipart request spec (the `Upload`
scalar registered via `AddType<UploadType>()`), so the generic JSON `graphql.ts` proxy cannot carry it.
`server/api/upload.ts` handles multipart pass-through separately. Max upload size is 5 MB
(`Constants.Blob.MaxBlobSizeInBytes`); allowed content types are `image/png`, `image/jpeg`,
`image/jpg`, `image/webp` — the client validates against the same list before uploading to fail fast.

### Pages

| Route | Purpose | Guard |
|---|---|---|
| `/` | Redirect to `/films` | — |
| `/films` | Paginated, filterable, sortable film list | — |
| `/films/[id]` | Film detail (actors, genres, director, producer) | — |
| `/films/new` | Create film form | `admin` |
| `/films/[id]/edit` | Edit film form | `moderator` |
| `/login` | Login form | — |
| `/register` | Registration form | — |

There is no single-film-by-id resolver on the API. The detail page fetches via
`films(where: { id: { eq: $id } })` and takes the first edge — no backend change needed.

Create/edit/delete are surfaced as role-gated action buttons on the list and detail pages rather than
a separate `/admin` section; a parallel admin tree is premature for a catalogue this size.

### UI

NuxtUI components throughout. Forms use `UForm` with its `schema` prop bound to zod schemas — no
separate validation library needed. Film list uses card/table components with NuxtUI's built-in
pagination bound to the Relay cursor connection.

### Aspire integration

`AppHost.cs` gains a Node app resource for `src/Frontend/MovieDatabase.Web` that references the API
project, so the GraphQL endpoint arrives as an injected environment variable (read into
`runtimeConfig` in `nuxt.config.ts`) rather than being hardcoded. The exact Aspire hosting call and
its current signature are deliberately not pinned here — the `aspireify` skill wires this at
implementation time.

A `.env.example` documents the same variable so the app can also run standalone with `pnpm dev`
against an already-running API.

### Testing

Vitest + `@nuxt/test-utils` + `@testing-library/vue`, covering composables (auth state, token refresh
decision logic, GraphQL composables) and key components (forms, film card, role-gated controls) in
isolation. Server proxy routes are tested with mocked downstream fetches.

**No E2E tests** — explicitly excluded from this project's scope.

## Out of scope

- **Attaching an uploaded image to a film.** `Film.Thumbnail` has no mutation write path — it was
  deliberately removed from `createFilm`/`editFilm` in a prior change. `uploadBlob` works standalone,
  so a reusable `ImageUploader` component is built, but nothing links its result to a film. As a
  result `thumbnailUrl` resolves to `null` for every film until a backend write path is designed.
- **Enforced server-side logout** — the API records revocation but does not enforce it when
  authenticating (see Authentication). Changing that is backend work.
- **Recovering a session idle past 60 minutes** — impossible given the refresh constraint above;
  the user logs in again.
- **E2E tests.**
- **Deployment / hosting configuration** — dev-loop only for now.

## Risks

- **Nuxt/NuxtUI major versions** are not pinned in this document; confirm at scaffold time and record
  the versions actually used.
- **The multipart upload proxy** is the least standard piece of the BFF layer and the most likely to
  need iteration.
