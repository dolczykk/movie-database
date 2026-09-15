# Nuxt Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a server-rendered Nuxt frontend for `MovieDatabase.Api` with film browsing, authentication, and role-based film management.

**Architecture:** The browser never calls the .NET GraphQL API directly. Because auth tokens live in HttpOnly cookies (unreadable by JavaScript), a thin backend-for-frontend layer of Nuxt server routes holds the cookies and attaches the `Authorization: Bearer` header when forwarding to the API. During SSR, Nuxt's server calls the API directly using the token from the incoming request's cookies. Role-gated pages are protected by route middleware reading a Pinia store populated from the decoded JWT.

**Tech Stack:** Nuxt (SSR), NuxtUI, Pinia, `@nuxtjs/apollo`, GraphQL Code Generator, zod, Vitest + `@testing-library/vue`, pnpm 10.14.0, Node v24.21.0.

**Spec:** `docs/superpowers/specs/2026-09-11-nuxt-frontend-design.md`

## Global Constraints

- **Frontend root:** `src/Frontend/MovieDatabase.Web`. All frontend paths in this plan are relative to it unless prefixed with `src/` or `test/`.
- **Package manager:** pnpm (10.14.0 installed). Never use `npm install` in this project.
- **Language/style:** TypeScript everywhere. Vue Composition API with `<script setup>`. No Options API.
- **State:** Pinia setup-style stores (`defineStore('name', () => {...})`), not options-style.
- **Role values** are exactly `Administrator`, `Moderator`, `User` — matching the API's `UserRoles` enum.
- **Page size:** API default 10, **max 20** (`Constants.Query.MaxPageSize`). Never request `first:` above 20.
- **Upload limits:** 5 MB max (`Constants.Blob.MaxBlobSizeInBytes`); allowed types `image/png`, `image/jpeg`, `image/jpg`, `image/webp`.
- **Token lifetimes:** access 60 min, refresh 7 days (`JwtSettings`).
- **Refresh requires a non-expired access token** — refresh proactively, never reactively after expiry. See Task 11.
- **Never commit secrets.** The API URL is config, not a secret; there are no frontend secrets in this plan.
- **Do not add the frontend to `MovieDatabase.sln`** — it is not an MSBuild project.

## Version Verification (do this once, in Task 2)

This plan was written against Nuxt 4.x / NuxtUI 3.x conventions. The scaffold may install newer majors. Two known churn points to check against the docs at scaffold time, and correct throughout if they differ:

1. **NuxtUI form field wrapper:** `UFormField` (v3+) vs `UFormGroup` (v2). Docs: https://ui.nuxt.com/components/form-field
2. **`@nuxtjs/apollo` Nuxt-major support.** If it does not support the installed Nuxt major, use the fallback in Task 13 — it is fully specified there, not a stub.

Record the actual installed versions in the Task 2 commit message.

---

## Phase 0 — Backend prerequisite

### Task 1: Fix the schema export tool so it emits the whole schema

`utils/MovieDatabase.GenerateSchema/Program.cs` registers only `FilmMutations` and `UserMutations`. The schema it produces is missing `revoke`, `uploadBlob`, and the `thumbnailUrl` field — all of which the frontend needs. Codegen would silently generate a type-safe client for the wrong API.

**Files:**
- Modify: `utils/MovieDatabase.GenerateSchema/Program.cs`
- Create: `test/MovieDatabase.IntegrationTests/schema.graphql` (generated output, committed)

**Interfaces:**
- Produces: `test/MovieDatabase.IntegrationTests/schema.graphql` — the SDL file every later codegen task reads.

- [ ] **Step 1: Make the tool's registrations match the real API**

Replace the service registration block in `utils/MovieDatabase.GenerateSchema/Program.cs` so it mirrors `src/Api/MovieDatabase.Api/Program.cs`:

```csharp
using HotChocolate.Execution;

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
```

Note: `RegisterDbContextFactory<AppDbContext>()` from the real `Program.cs` is deliberately omitted — schema printing does not execute resolvers, so no database is needed.

- [ ] **Step 2: Run the tool from the repository root**

The output path is relative to the current directory, so this must run from the repo root:

```bash
cd "$(git rev-parse --show-toplevel)"
dotnet run --project utils/MovieDatabase.GenerateSchema/MovieDatabase.GenerateSchema.csproj
```

Expected: `Generated schema at .../test/MovieDatabase.IntegrationTests/schema.graphql`

If it fails to build, the most likely cause is a missing `ProjectReference` — the tool already references `MovieDatabase.Api`, which transitively provides `Core` and `Types`.

- [ ] **Step 3: Verify the schema contains the previously-missing members**

```bash
cd "$(git rev-parse --show-toplevel)"
grep -E "revoke|uploadBlob|thumbnailUrl" test/MovieDatabase.IntegrationTests/schema.graphql
```

Expected: all three appear. If `thumbnailUrl` is missing, `FilmTypeExtensions` was not registered.

- [ ] **Step 4: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add utils/MovieDatabase.GenerateSchema/Program.cs test/MovieDatabase.IntegrationTests/schema.graphql
git commit -m "$(cat <<'EOF'
fix(tools): register all type extensions in schema export

The generated SDL was missing revoke, uploadBlob and thumbnailUrl because
the tool's GraphQL registration had drifted from the API's Program.cs.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 1 — Project foundation

### Task 2: Scaffold the Nuxt project

**Files:**
- Create: `src/Frontend/MovieDatabase.Web/` (whole scaffold)
- Modify: `src/Frontend/MovieDatabase.Web/nuxt.config.ts`
- Create: `src/Frontend/MovieDatabase.Web/.env.example`
- Modify: `.gitignore` (repo root)

**Interfaces:**
- Produces: `runtimeConfig.graphqlEndpoint` (server-only, string) — the API's `/graphql` URL, consumed by every server route. Deliberately not under `public`: the browser never needs it, and never calls the API directly.

- [ ] **Step 1: Scaffold**

```bash
cd "$(git rev-parse --show-toplevel)/src"
mkdir -p Frontend
cd Frontend
pnpm create nuxt@latest MovieDatabase.Web --package-manager pnpm --no-install --no-git-init
cd MovieDatabase.Web
pnpm install
```

Answer any template prompts with the defaults. Record the Nuxt version from `package.json` — it goes in the commit message.

- [ ] **Step 2: Add runtime dependencies**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm add @nuxt/ui @pinia/nuxt pinia zod
```

- [ ] **Step 3: Configure Nuxt**

Replace `nuxt.config.ts`:

```ts
export default defineNuxtConfig({
  compatibilityDate: '2026-09-11',
  devtools: { enabled: true },
  modules: ['@nuxt/ui', '@pinia/nuxt'],
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    // Server-only. Overridden by NUXT_GRAPHQL_ENDPOINT, which Aspire injects (Task 20).
    graphqlEndpoint: 'https://localhost:7204/graphql',
  },
})
```

Create `assets/css/main.css`:

```css
@import "tailwindcss";
@import "@nuxt/ui";
```

Check https://ui.nuxt.com/getting-started/installation/nuxt for the installed NuxtUI major — if it predates the Tailwind v4 CSS-first setup, follow its documented install steps instead of the two `@import` lines above.

- [ ] **Step 4: Document the environment variable**

Create `.env.example`:

```
# The MovieDatabase.Api GraphQL endpoint.
# When run under the Aspire AppHost this is injected automatically (see AppHost.cs).
# Set it manually only when running `pnpm dev` standalone against an already-running API.
NUXT_GRAPHQL_ENDPOINT=https://localhost:7204/graphql
```

- [ ] **Step 5: Ignore build output from the repo root**

Append to the repo-root `.gitignore`:

```
# Nuxt frontend
src/Frontend/MovieDatabase.Web/node_modules/
src/Frontend/MovieDatabase.Web/.nuxt/
src/Frontend/MovieDatabase.Web/.output/
src/Frontend/MovieDatabase.Web/dist/
```

- [ ] **Step 6: Verify it builds and serves**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm build
```

Expected: build completes with no errors. Then:

```bash
pnpm dev
```

Expected: dev server starts and `http://localhost:3000` renders the welcome page. Stop it with Ctrl+C.

- [ ] **Step 7: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web .gitignore
git commit -m "$(cat <<'EOF'
feat(web): scaffold Nuxt frontend with NuxtUI and Pinia

Records installed versions: Nuxt <X.Y.Z>, NuxtUI <X.Y.Z>.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

Replace `<X.Y.Z>` with the real versions from `package.json`.

---

### Task 3: Set up the Vitest harness

**Files:**
- Create: `vitest.config.ts`
- Create: `test/setup.ts`
- Create: `test/smoke.spec.ts`
- Modify: `package.json` (scripts)

**Interfaces:**
- Produces: `pnpm test` (single run) and `pnpm test:watch` — used by every later task.

- [ ] **Step 1: Install test dependencies**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm add -D vitest @vue/test-utils @testing-library/vue @nuxt/test-utils happy-dom
```

- [ ] **Step 2: Configure Vitest**

Create `vitest.config.ts`:

```ts
import { defineVitestConfig } from '@nuxt/test-utils/config'

export default defineVitestConfig({
  test: {
    environment: 'happy-dom',
    globals: true,
    setupFiles: ['./test/setup.ts'],
    include: ['test/**/*.spec.ts'],
  },
})
```

Create `test/setup.ts`:

```ts
import '@testing-library/vue'
```

- [ ] **Step 3: Add scripts**

In `package.json`, add to `"scripts"`:

```json
"test": "vitest run",
"test:watch": "vitest"
```

- [ ] **Step 4: Write a smoke test**

Create `test/smoke.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'

describe('test harness', () => {
  it('runs', () => {
    expect(1 + 1).toBe(2)
  })
})
```

- [ ] **Step 5: Run it**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test
```

Expected: 1 passed.

- [ ] **Step 6: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
test(web): add Vitest harness

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Wire GraphQL Code Generator

**Files:**
- Create: `codegen.ts`
- Create: `graphql/queries/films.graphql`
- Create: `graphql/mutations/auth.graphql`
- Modify: `package.json` (scripts)
- Modify: `.gitignore` (frontend-local)

**Interfaces:**
- Produces: `graphql/generated/graphql.ts` exporting typed document nodes and TypeScript types for every operation. Later tasks import operation documents from here.
- Consumes: `test/MovieDatabase.IntegrationTests/schema.graphql` from Task 1.

The operation documents below are adapted from the working documents already in `test/MovieDatabase.IntegrationTests/Graphql/`, which are exercised by the integration test suite — the argument names and input type names are therefore known-correct, not guessed.

- [ ] **Step 1: Install codegen**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm add -D @graphql-codegen/cli @graphql-codegen/client-preset
```

- [ ] **Step 2: Configure codegen**

Create `codegen.ts`:

```ts
import type { CodegenConfig } from '@graphql-codegen/cli'

const config: CodegenConfig = {
  schema: '../../../test/MovieDatabase.IntegrationTests/schema.graphql',
  documents: ['graphql/**/*.graphql'],
  ignoreNoDocuments: true,
  generates: {
    'graphql/generated/': {
      preset: 'client',
      config: {
        useTypeImports: true,
      },
    },
  },
}

export default config
```

- [ ] **Step 3: Write the film query documents**

Create `graphql/queries/films.graphql`:

```graphql
query GetFilms($first: Int!, $after: String, $where: FilmFilterInput, $order: [FilmSortInput!]) {
  films(first: $first, after: $after, where: $where, order: $order) {
    nodes {
      id
      title
      description
      releaseDate
      thumbnailUrl
      director {
        id
        name
        surname
      }
      genres {
        id
        name
      }
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}

query GetFilmById($id: UUID!) {
  films(first: 1, where: { id: { eq: $id } }) {
    nodes {
      id
      title
      description
      releaseDate
      thumbnailUrl
      actors {
        id
        name
        surname
      }
      genres {
        id
        name
      }
      director {
        id
        name
        surname
      }
      producer {
        id
        name
      }
    }
  }
}
```

- [ ] **Step 4: Write the auth mutation documents**

Create `graphql/mutations/auth.graphql`:

```graphql
mutation LoginUser($request: AuthenticateUserRequestInput!) {
  loginUser(request: $request) {
    id
    username
    email
    role
    token
    expireTime
    refreshToken
    refreshTokenExpireTime
  }
}

mutation RegisterUser($request: CreateUserRequestInput!) {
  registerUser(request: $request) {
    id
    username
    email
    role
    token
    expireTime
    refreshToken
    refreshTokenExpireTime
  }
}

mutation RevokeToken($input: RevokeTokenRequestInput!) {
  revoke(input: $input) {
    accessToken {
      value
      expiresAt
    }
    refreshToken {
      value
      expiresAt
    }
  }
}
```

- [ ] **Step 5: Add the script and run codegen**

Add to `package.json` scripts:

```json
"codegen": "graphql-codegen --config codegen.ts"
```

Then:

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm codegen
```

Expected: files appear under `graphql/generated/`.

If codegen reports an unknown scalar or field — most likely `UUID` for the film id, or `thumbnailUrl` — open `test/MovieDatabase.IntegrationTests/schema.graphql` and match the exact scalar and field names it declares. The SDL is authoritative; correct the documents to match it rather than the other way round.

- [ ] **Step 6: Verify the generated types are usable**

Create `test/codegen.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { GetFilmsDocument, LoginUserDocument } from '../graphql/generated/graphql'

describe('generated graphql documents', () => {
  it('exports the film query document', () => {
    expect(GetFilmsDocument).toBeDefined()
  })

  it('exports the login mutation document', () => {
    expect(LoginUserDocument).toBeDefined()
  })
})
```

```bash
pnpm test
```

Expected: all tests pass. If the import path differs, check what `graphql/generated/` actually contains and correct the import.

- [ ] **Step 7: Commit**

Generated output is committed so the app builds without a running API.

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): generate typed GraphQL client from API schema

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Auth server layer (backend-for-frontend)

### Task 5: JWT payload decoding

A pure function, shared by server routes and client store. Placed in a plain `lib/` directory with explicit imports rather than relying on Nuxt auto-import directories, so it works identically from `server/` and `app/` on any Nuxt major.

**Files:**
- Create: `lib/jwt.ts`
- Test: `test/lib/jwt.spec.ts`

**Interfaces:**
- Produces: `decodeJwtPayload(token: string): JwtPayload | null`
- Produces: `isExpired(payload: JwtPayload, skewSeconds?: number, now?: number): boolean`
- Produces: `interface JwtPayload { jti: string; sub: string; email: string; role: string | null; exp: number }`

`role` reads either claim key. `JwtService.GenerateJwtToken` emits `new Claim(ClaimTypes.Role, ...)`, and whether that serializes as `role` or the long `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` URI depends on `JwtSecurityTokenHandler`'s outbound claim map. Reading both removes the guess.

- [ ] **Step 1: Write the failing tests**

Create `test/lib/jwt.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { decodeJwtPayload, isExpired } from '../../lib/jwt'

function makeToken(payload: Record<string, unknown>): string {
  const encode = (value: object) =>
    Buffer.from(JSON.stringify(value))
      .toString('base64')
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '')
  return `${encode({ alg: 'HS256' })}.${encode(payload)}.signature`
}

describe('decodeJwtPayload', () => {
  it('reads the standard claims', () => {
    const token = makeToken({
      jti: 'user-1',
      sub: 'dolczyk',
      email: 'd@example.com',
      role: 'Administrator',
      exp: 1893456000,
    })

    expect(decodeJwtPayload(token)).toEqual({
      jti: 'user-1',
      sub: 'dolczyk',
      email: 'd@example.com',
      role: 'Administrator',
      exp: 1893456000,
    })
  })

  it('reads the role from the schema URI claim key', () => {
    const token = makeToken({
      jti: 'user-1',
      sub: 'dolczyk',
      email: 'd@example.com',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Moderator',
      exp: 1893456000,
    })

    expect(decodeJwtPayload(token)?.role).toBe('Moderator')
  })

  it('returns null role when no role claim is present', () => {
    const token = makeToken({ jti: 'u', sub: 's', email: 'e', exp: 1893456000 })

    expect(decodeJwtPayload(token)?.role).toBeNull()
  })

  it('returns null for a malformed token', () => {
    expect(decodeJwtPayload('not-a-jwt')).toBeNull()
    expect(decodeJwtPayload('a.b')).toBeNull()
  })

  it('returns null when exp is missing', () => {
    expect(decodeJwtPayload(makeToken({ jti: 'u' }))).toBeNull()
  })
})

describe('isExpired', () => {
  const payload = { jti: 'u', sub: 's', email: 'e', role: null, exp: 1_000 }

  it('is false well before expiry', () => {
    expect(isExpired(payload, 0, 900_000)).toBe(false)
  })

  it('is true at expiry', () => {
    expect(isExpired(payload, 0, 1_000_000)).toBe(true)
  })

  it('is true inside the skew window', () => {
    expect(isExpired(payload, 60, 950_000)).toBe(true)
  })
})
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/lib/jwt.spec.ts
```

Expected: FAIL — cannot resolve `../../lib/jwt`.

- [ ] **Step 3: Implement**

Create `lib/jwt.ts`:

```ts
const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export interface JwtPayload {
  /** The API puts the user id in the jti claim. */
  jti: string
  sub: string
  email: string
  role: string | null
  /** Seconds since the Unix epoch. */
  exp: number
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const parts = token.split('.')
  if (parts.length !== 3) {
    return null
  }

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    const raw = JSON.parse(atob(base64)) as Record<string, unknown>

    if (typeof raw.exp !== 'number') {
      return null
    }

    const role = raw.role ?? raw[ROLE_CLAIM_URI]

    return {
      jti: String(raw.jti ?? ''),
      sub: String(raw.sub ?? ''),
      email: String(raw.email ?? ''),
      role: typeof role === 'string' ? role : null,
      exp: raw.exp,
    }
  }
  catch {
    return null
  }
}

export function isExpired(payload: JwtPayload, skewSeconds = 0, now = Date.now()): boolean {
  return payload.exp * 1000 <= now + skewSeconds * 1000
}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
pnpm test test/lib/jwt.spec.ts
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): decode JWT payloads for auth state

Reads the role from both the short and schema-URI claim keys, since the
emitted key depends on JwtSecurityTokenHandler's outbound claim map.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Server cookie helpers and API caller

**Files:**
- Create: `server/utils/authCookies.ts`
- Create: `server/utils/graphqlClient.ts`
- Test: `test/server/authCookies.spec.ts`

**Interfaces:**
- Produces: `ACCESS_TOKEN_COOKIE`, `REFRESH_TOKEN_COOKIE` (string constants)
- Produces: `setAuthCookies(event: H3Event, tokens: TokenPair): void`
- Produces: `clearAuthCookies(event: H3Event): void`
- Produces: `readAuthCookies(event: H3Event): { accessToken: string | null; refreshToken: string | null }`
- Produces: `interface TokenPair { accessToken: string; accessTokenExpiresAt: string; refreshToken: string; refreshTokenExpiresAt: string }`
- Produces: `callGraphql<T>(event: H3Event, query: string, variables: Record<string, unknown>, accessToken?: string | null): Promise<T>` — throws on GraphQL errors.

`callGraphql` throws on errors because auth routes want failures as HTTP errors. The proxy in Task 9 deliberately does *not* use it — it passes the raw GraphQL envelope through untouched.

- [ ] **Step 1: Write the failing test**

Create `test/server/authCookies.spec.ts`:

```ts
import { describe, expect, it, vi } from 'vitest'

const setCookie = vi.fn()
const getCookie = vi.fn()
const deleteCookie = vi.fn()

vi.mock('h3', () => ({ setCookie, getCookie, deleteCookie }))

const { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE, setAuthCookies, clearAuthCookies, readAuthCookies }
  = await import('../../server/utils/authCookies')

const event = {} as never

describe('setAuthCookies', () => {
  it('writes both tokens as httpOnly cookies expiring with the token', () => {
    setCookie.mockClear()

    setAuthCookies(event, {
      accessToken: 'access',
      accessTokenExpiresAt: '2026-09-11T10:00:00Z',
      refreshToken: 'refresh',
      refreshTokenExpiresAt: '2026-09-18T09:00:00Z',
    })

    expect(setCookie).toHaveBeenCalledTimes(2)

    const [, accessName, accessValue, accessOptions] = setCookie.mock.calls[0]
    expect(accessName).toBe(ACCESS_TOKEN_COOKIE)
    expect(accessValue).toBe('access')
    expect(accessOptions.httpOnly).toBe(true)
    expect(accessOptions.sameSite).toBe('lax')
    expect(accessOptions.path).toBe('/')
    expect(accessOptions.expires).toEqual(new Date('2026-09-11T10:00:00Z'))

    const [, refreshName, refreshValue] = setCookie.mock.calls[1]
    expect(refreshName).toBe(REFRESH_TOKEN_COOKIE)
    expect(refreshValue).toBe('refresh')
  })
})

describe('clearAuthCookies', () => {
  it('deletes both cookies', () => {
    deleteCookie.mockClear()

    clearAuthCookies(event)

    expect(deleteCookie.mock.calls.map(call => call[1]))
      .toEqual([ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE])
  })
})

describe('readAuthCookies', () => {
  it('returns null for absent cookies', () => {
    getCookie.mockReturnValue(undefined)

    expect(readAuthCookies(event)).toEqual({ accessToken: null, refreshToken: null })
  })
})
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/server/authCookies.spec.ts
```

Expected: FAIL — cannot resolve `../../server/utils/authCookies`.

- [ ] **Step 3: Implement the cookie helpers**

Create `server/utils/authCookies.ts`:

```ts
import type { H3Event } from 'h3'
import { deleteCookie, getCookie, setCookie } from 'h3'

export const ACCESS_TOKEN_COOKIE = 'md_access_token'
export const REFRESH_TOKEN_COOKIE = 'md_refresh_token'

export interface TokenPair {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

function baseOptions() {
  return {
    httpOnly: true,
    secure: !import.meta.dev,
    sameSite: 'lax' as const,
    path: '/',
  }
}

export function setAuthCookies(event: H3Event, tokens: TokenPair): void {
  setCookie(event, ACCESS_TOKEN_COOKIE, tokens.accessToken, {
    ...baseOptions(),
    expires: new Date(tokens.accessTokenExpiresAt),
  })
  setCookie(event, REFRESH_TOKEN_COOKIE, tokens.refreshToken, {
    ...baseOptions(),
    expires: new Date(tokens.refreshTokenExpiresAt),
  })
}

export function clearAuthCookies(event: H3Event): void {
  deleteCookie(event, ACCESS_TOKEN_COOKIE, { path: '/' })
  deleteCookie(event, REFRESH_TOKEN_COOKIE, { path: '/' })
}

export function readAuthCookies(event: H3Event): { accessToken: string | null, refreshToken: string | null } {
  return {
    accessToken: getCookie(event, ACCESS_TOKEN_COOKIE) ?? null,
    refreshToken: getCookie(event, REFRESH_TOKEN_COOKIE) ?? null,
  }
}
```

- [ ] **Step 4: Implement the GraphQL caller**

Create `server/utils/graphqlClient.ts`:

```ts
import type { H3Event } from 'h3'
import { createError } from 'h3'

interface GraphqlEnvelope<T> {
  data?: T
  errors?: Array<{ message: string }>
}

export async function callGraphql<T>(
  event: H3Event,
  query: string,
  variables: Record<string, unknown>,
  accessToken?: string | null,
): Promise<T> {
  const endpoint = useRuntimeConfig(event).graphqlEndpoint

  const response = await $fetch<GraphqlEnvelope<T>>(endpoint, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
    },
    body: { query, variables },
  })

  if (response.errors?.length) {
    throw createError({
      statusCode: 400,
      statusMessage: response.errors[0].message,
    })
  }

  if (!response.data) {
    throw createError({ statusCode: 502, statusMessage: 'Empty GraphQL response' })
  }

  return response.data
}
```

- [ ] **Step 5: Run the tests to verify they pass**

```bash
pnpm test test/server/authCookies.spec.ts
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add server-side auth cookie helpers and GraphQL caller

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Login, register, logout and me routes

**Files:**
- Create: `server/utils/authOperations.ts`
- Create: `server/api/auth/login.post.ts`
- Create: `server/api/auth/register.post.ts`
- Create: `server/api/auth/logout.post.ts`
- Create: `server/api/auth/me.get.ts`
- Test: `test/server/authOperations.spec.ts`

**Interfaces:**
- Produces: `interface AuthUser { id: string; username: string; email: string; role: string | null }` — the only user shape ever sent to the browser. Token fields are stripped.
- Produces: `toTokenPair(credentials): TokenPair` — maps the API's `UserCredentialsDto` field names onto `TokenPair`.
- Produces: `POST /api/auth/login` body `{ email, password }` → `AuthUser`
- Produces: `POST /api/auth/register` body `{ username, email, password }` → `AuthUser`
- Produces: `POST /api/auth/logout` → `{ ok: true }`
- Produces: `GET /api/auth/me` → `AuthUser | null`

`UserCredentialsDto` names its fields `token`/`expireTime`/`refreshToken`/`refreshTokenExpireTime`, while `RevokeTokenDto` (Task 8) uses `accessToken.value`/`accessToken.expiresAt`. `toTokenPair` exists so the rest of the code sees one shape.

- [ ] **Step 1: Write the failing test**

Create `test/server/authOperations.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { toAuthUser, toTokenPair } from '../../server/utils/authOperations'

const credentials = {
  id: 'user-1',
  username: 'dolczyk',
  email: 'd@example.com',
  role: 'Administrator',
  token: 'access-token',
  expireTime: '2026-09-11T10:00:00Z',
  refreshToken: 'refresh-token',
  refreshTokenExpireTime: '2026-09-18T09:00:00Z',
}

describe('toTokenPair', () => {
  it('maps UserCredentialsDto field names onto the shared shape', () => {
    expect(toTokenPair(credentials)).toEqual({
      accessToken: 'access-token',
      accessTokenExpiresAt: '2026-09-11T10:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAt: '2026-09-18T09:00:00Z',
    })
  })
})

describe('toAuthUser', () => {
  it('keeps only the non-sensitive fields', () => {
    expect(toAuthUser(credentials)).toEqual({
      id: 'user-1',
      username: 'dolczyk',
      email: 'd@example.com',
      role: 'Administrator',
    })
  })

  it('never leaks tokens', () => {
    expect(JSON.stringify(toAuthUser(credentials))).not.toContain('access-token')
    expect(JSON.stringify(toAuthUser(credentials))).not.toContain('refresh-token')
  })
})
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/server/authOperations.spec.ts
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement the mappers**

Create `server/utils/authOperations.ts`:

```ts
import type { TokenPair } from './authCookies'

export interface UserCredentials {
  id: string
  username: string
  email: string
  role: string | null
  token: string
  expireTime: string
  refreshToken: string
  refreshTokenExpireTime: string
}

export interface AuthUser {
  id: string
  username: string
  email: string
  role: string | null
}

export function toTokenPair(credentials: UserCredentials): TokenPair {
  return {
    accessToken: credentials.token,
    accessTokenExpiresAt: credentials.expireTime,
    refreshToken: credentials.refreshToken,
    refreshTokenExpiresAt: credentials.refreshTokenExpireTime,
  }
}

export function toAuthUser(credentials: UserCredentials): AuthUser {
  return {
    id: credentials.id,
    username: credentials.username,
    email: credentials.email,
    role: credentials.role,
  }
}

export const LOGIN_USER_MUTATION = `
  mutation LoginUser($request: AuthenticateUserRequestInput!) {
    loginUser(request: $request) {
      id username email role
      token expireTime refreshToken refreshTokenExpireTime
    }
  }
`

export const REGISTER_USER_MUTATION = `
  mutation RegisterUser($request: CreateUserRequestInput!) {
    registerUser(request: $request) {
      id username email role
      token expireTime refreshToken refreshTokenExpireTime
    }
  }
`

export const REVOKE_TOKEN_MUTATION = `
  mutation RevokeToken($input: RevokeTokenRequestInput!) {
    revoke(input: $input) {
      accessToken { value expiresAt }
      refreshToken { value expiresAt }
    }
  }
`
```

These mutation strings are inlined rather than imported from `graphql/generated/` because Nitro's server bundle should not depend on the client preset's runtime. They mirror `graphql/mutations/auth.graphql` exactly — if you change one, change both.

- [ ] **Step 4: Implement the login route**

Create `server/api/auth/login.post.ts`:

```ts
import { z } from 'zod'
import { setAuthCookies } from '../../utils/authCookies'
import { callGraphql } from '../../utils/graphqlClient'
import { LOGIN_USER_MUTATION, toAuthUser, toTokenPair, type UserCredentials } from '../../utils/authOperations'

const bodySchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
})

export default defineEventHandler(async (event) => {
  const body = await readValidatedBody(event, bodySchema.parse)

  const data = await callGraphql<{ loginUser: UserCredentials }>(
    event,
    LOGIN_USER_MUTATION,
    { request: { email: body.email, password: body.password } },
  )

  setAuthCookies(event, toTokenPair(data.loginUser))

  return toAuthUser(data.loginUser)
})
```

- [ ] **Step 5: Implement the register route**

Create `server/api/auth/register.post.ts`:

```ts
import { z } from 'zod'
import { setAuthCookies } from '../../utils/authCookies'
import { callGraphql } from '../../utils/graphqlClient'
import { REGISTER_USER_MUTATION, toAuthUser, toTokenPair, type UserCredentials } from '../../utils/authOperations'

const bodySchema = z.object({
  username: z.string().min(1),
  email: z.string().email(),
  password: z.string().min(8),
})

export default defineEventHandler(async (event) => {
  const body = await readValidatedBody(event, bodySchema.parse)

  const data = await callGraphql<{ registerUser: UserCredentials }>(
    event,
    REGISTER_USER_MUTATION,
    { request: { username: body.username, email: body.email, password: body.password } },
  )

  setAuthCookies(event, toTokenPair(data.registerUser))

  return toAuthUser(data.registerUser)
})
```

- [ ] **Step 6: Implement logout and me**

Create `server/api/auth/logout.post.ts`:

```ts
import { clearAuthCookies } from '../../utils/authCookies'

export default defineEventHandler((event) => {
  clearAuthCookies(event)
  return { ok: true }
})
```

Create `server/api/auth/me.get.ts`:

```ts
import { decodeJwtPayload, isExpired } from '../../../lib/jwt'
import { readAuthCookies } from '../../utils/authCookies'
import type { AuthUser } from '../../utils/authOperations'

export default defineEventHandler((event): AuthUser | null => {
  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    return null
  }

  const payload = decodeJwtPayload(accessToken)
  if (!payload || isExpired(payload)) {
    return null
  }

  return {
    id: payload.jti,
    username: payload.sub,
    email: payload.email,
    role: payload.role,
  }
})
```

- [ ] **Step 7: Run the tests to verify they pass**

```bash
pnpm test test/server/authOperations.spec.ts
```

Expected: all pass.

- [ ] **Step 8: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add login, register, logout and me server routes

Tokens are set as httpOnly cookies server-side; only non-sensitive user
fields are returned to the browser.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: Single-flight token refresh route

The API rotates refresh tokens: `FindUserToRevokeToken` matches only `!IsRevoked`, and the handler revokes the presented pair before storing the new one. Two concurrent refreshes with the same pair means one wins and one hard-fails, logging the user out spuriously. The route therefore deduplicates concurrent refreshes keyed on the refresh token.

**Files:**
- Create: `server/utils/singleFlight.ts`
- Create: `server/api/auth/refresh.post.ts`
- Test: `test/server/singleFlight.spec.ts`

**Interfaces:**
- Produces: `singleFlight<T>(key: string, fn: () => Promise<T>): Promise<T>`
- Produces: `POST /api/auth/refresh` → `AuthUser`; 401 when no cookies or the API rejects the pair.

- [ ] **Step 1: Write the failing test**

Create `test/server/singleFlight.spec.ts`:

```ts
import { describe, expect, it, vi } from 'vitest'
import { singleFlight } from '../../server/utils/singleFlight'

describe('singleFlight', () => {
  it('runs the function once for concurrent calls with the same key', async () => {
    const fn = vi.fn(async () => {
      await new Promise(resolve => setTimeout(resolve, 10))
      return 'result'
    })

    const [first, second] = await Promise.all([
      singleFlight('key', fn),
      singleFlight('key', fn),
    ])

    expect(fn).toHaveBeenCalledTimes(1)
    expect(first).toBe('result')
    expect(second).toBe('result')
  })

  it('runs the function separately for different keys', async () => {
    const fn = vi.fn(async () => 'result')

    await Promise.all([singleFlight('a', fn), singleFlight('b', fn)])

    expect(fn).toHaveBeenCalledTimes(2)
  })

  it('allows a new call after the previous one settles', async () => {
    const fn = vi.fn(async () => 'result')

    await singleFlight('key', fn)
    await singleFlight('key', fn)

    expect(fn).toHaveBeenCalledTimes(2)
  })

  it('does not cache rejections', async () => {
    const failing = vi.fn(async () => {
      throw new Error('boom')
    })

    await expect(singleFlight('key', failing)).rejects.toThrow('boom')
    await expect(singleFlight('key', failing)).rejects.toThrow('boom')

    expect(failing).toHaveBeenCalledTimes(2)
  })
})
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/server/singleFlight.spec.ts
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement single-flight**

Create `server/utils/singleFlight.ts`:

```ts
const inFlight = new Map<string, Promise<unknown>>()

export function singleFlight<T>(key: string, fn: () => Promise<T>): Promise<T> {
  const existing = inFlight.get(key) as Promise<T> | undefined
  if (existing) {
    return existing
  }

  const promise = fn().finally(() => {
    inFlight.delete(key)
  })

  inFlight.set(key, promise)

  return promise
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
pnpm test test/server/singleFlight.spec.ts
```

Expected: all 4 pass.

- [ ] **Step 5: Implement the refresh route**

Create `server/api/auth/refresh.post.ts`:

```ts
import { createError } from 'h3'
import { decodeJwtPayload } from '../../../lib/jwt'
import { clearAuthCookies, readAuthCookies, setAuthCookies, type TokenPair } from '../../utils/authCookies'
import { callGraphql } from '../../utils/graphqlClient'
import { REVOKE_TOKEN_MUTATION } from '../../utils/authOperations'
import { singleFlight } from '../../utils/singleFlight'
import type { AuthUser } from '../../utils/authOperations'

interface RevokeResult {
  revoke: {
    accessToken: { value: string, expiresAt: string }
    refreshToken: { value: string, expiresAt: string }
  }
}

export default defineEventHandler(async (event): Promise<AuthUser> => {
  const { accessToken, refreshToken } = readAuthCookies(event)

  if (!accessToken || !refreshToken) {
    throw createError({ statusCode: 401, statusMessage: 'Not authenticated' })
  }

  let pair: TokenPair
  try {
    pair = await singleFlight(refreshToken, async () => {
      const data = await callGraphql<RevokeResult>(
        event,
        REVOKE_TOKEN_MUTATION,
        { input: { accessToken, refreshToken } },
      )

      return {
        accessToken: data.revoke.accessToken.value,
        accessTokenExpiresAt: data.revoke.accessToken.expiresAt,
        refreshToken: data.revoke.refreshToken.value,
        refreshTokenExpiresAt: data.revoke.refreshToken.expiresAt,
      }
    })
  }
  catch {
    // The API rejects refresh once the access token has expired, since
    // ReadPrincipalFromExpiredToken validates lifetime. The session is over.
    clearAuthCookies(event)
    throw createError({ statusCode: 401, statusMessage: 'Session expired' })
  }

  setAuthCookies(event, pair)

  const payload = decodeJwtPayload(pair.accessToken)
  if (!payload) {
    throw createError({ statusCode: 502, statusMessage: 'Unreadable token from API' })
  }

  return {
    id: payload.jti,
    username: payload.sub,
    email: payload.email,
    role: payload.role,
  }
})
```

- [ ] **Step 6: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add single-flight token refresh route

The API rotates and single-uses refresh tokens, so concurrent refreshes
with the same pair would spuriously log the user out. Dedupe them.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 9: GraphQL proxy route

The browser's GraphQL traffic goes through here so the HttpOnly cookie can become a Bearer header. Unlike `callGraphql`, this passes the raw GraphQL envelope back untouched so the Apollo client sees normal GraphQL errors.

**Files:**
- Create: `server/api/graphql.post.ts`

**Interfaces:**
- Produces: `POST /api/graphql` — accepts and returns a standard GraphQL-over-HTTP JSON envelope.

- [ ] **Step 1: Implement the proxy**

Create `server/api/graphql.post.ts`:

```ts
import { readAuthCookies } from '../utils/authCookies'

interface GraphqlRequest {
  query: string
  variables?: Record<string, unknown>
  operationName?: string
}

export default defineEventHandler(async (event) => {
  const body = await readBody<GraphqlRequest>(event)
  const { accessToken } = readAuthCookies(event)
  const endpoint = useRuntimeConfig(event).graphqlEndpoint

  return await $fetch(endpoint, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
    },
    body,
  })
})
```

- [ ] **Step 2: Verify it forwards against the running API**

Start the API (from the repo root, in a separate terminal):

```bash
cd "$(git rev-parse --show-toplevel)"
dotnet run --project src/Boostrapper/MovieDatabase.AppHost/MovieDatabase.AppHost.csproj
```

Note the API's URL from the Aspire dashboard and set `NUXT_GRAPHQL_ENDPOINT` in a local `.env` to `<that url>/graphql`. Then:

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm dev
```

In a third terminal:

```bash
curl -s http://localhost:3000/api/graphql \
  -H 'content-type: application/json' \
  -d '{"query":"{ films(first: 3) { nodes { id title } } }"}'
```

Expected: a JSON envelope containing film nodes. An empty `nodes` array is fine (seed data may vary); an `errors` array means the endpoint URL is wrong.

- [ ] **Step 3: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): proxy browser GraphQL traffic through the Nuxt server

The browser cannot read the httpOnly token cookie, so the server attaches
the bearer header on its behalf.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — Client auth

### Task 10: Auth store and initialisation

**Files:**
- Create: `app/stores/auth.ts`
- Create: `app/plugins/auth.ts`
- Test: `test/stores/auth.spec.ts`

**Interfaces:**
- Produces: `useAuthStore()` with state `user: Ref<AuthUser | null>` and getters `isAuthenticated`, `isAdministrator`, `canManageFilms` (Administrator or Moderator), plus actions `setUser(user)`, `login(email, password)`, `register(username, email, password)`, `logout()`, `refresh()`.

`canManageFilms` is deliberately named for the capability, not the role, because two roles grant it — `editFilm` allows Administrator and Moderator.

- [ ] **Step 1: Write the failing test**

Create `test/stores/auth.spec.ts`:

```ts
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from '../../app/stores/auth'

beforeEach(() => {
  setActivePinia(createPinia())
})

const administrator = { id: '1', username: 'admin', email: 'a@example.com', role: 'Administrator' }
const moderator = { id: '2', username: 'mod', email: 'm@example.com', role: 'Moderator' }
const plainUser = { id: '3', username: 'user', email: 'u@example.com', role: 'User' }

describe('useAuthStore', () => {
  it('starts logged out', () => {
    const store = useAuthStore()

    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
    expect(store.isAdministrator).toBe(false)
    expect(store.canManageFilms).toBe(false)
  })

  it('grants everything to an administrator', () => {
    const store = useAuthStore()
    store.setUser(administrator)

    expect(store.isAuthenticated).toBe(true)
    expect(store.isAdministrator).toBe(true)
    expect(store.canManageFilms).toBe(true)
  })

  it('lets a moderator manage films but not administer', () => {
    const store = useAuthStore()
    store.setUser(moderator)

    expect(store.isAdministrator).toBe(false)
    expect(store.canManageFilms).toBe(true)
  })

  it('grants a plain user neither', () => {
    const store = useAuthStore()
    store.setUser(plainUser)

    expect(store.isAuthenticated).toBe(true)
    expect(store.isAdministrator).toBe(false)
    expect(store.canManageFilms).toBe(false)
  })

  it('clears the user on setUser(null)', () => {
    const store = useAuthStore()
    store.setUser(administrator)
    store.setUser(null)

    expect(store.isAuthenticated).toBe(false)
  })
})
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/stores/auth.spec.ts
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement the store**

Create `app/stores/auth.ts`:

```ts
import { defineStore } from 'pinia'

export interface AuthUser {
  id: string
  username: string
  email: string
  role: string | null
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)

  const isAuthenticated = computed(() => user.value !== null)
  const isAdministrator = computed(() => user.value?.role === 'Administrator')
  const canManageFilms = computed(
    () => user.value?.role === 'Administrator' || user.value?.role === 'Moderator',
  )

  function setUser(next: AuthUser | null): void {
    user.value = next
  }

  async function login(email: string, password: string): Promise<void> {
    user.value = await $fetch<AuthUser>('/api/auth/login', {
      method: 'POST',
      body: { email, password },
    })
  }

  async function register(username: string, email: string, password: string): Promise<void> {
    user.value = await $fetch<AuthUser>('/api/auth/register', {
      method: 'POST',
      body: { username, email, password },
    })
  }

  async function logout(): Promise<void> {
    await $fetch('/api/auth/logout', { method: 'POST' })
    user.value = null
  }

  async function refresh(): Promise<boolean> {
    try {
      user.value = await $fetch<AuthUser>('/api/auth/refresh', { method: 'POST' })
      return true
    }
    catch {
      user.value = null
      return false
    }
  }

  return { user, isAuthenticated, isAdministrator, canManageFilms, setUser, login, register, logout, refresh }
})
```

If `ref`/`computed` are not auto-imported in the test environment, add `import { computed, ref } from 'vue'` at the top.

- [ ] **Step 4: Run the test to verify it passes**

```bash
pnpm test test/stores/auth.spec.ts
```

Expected: all 5 pass.

- [ ] **Step 5: Populate the store during SSR**

Create `app/plugins/auth.ts`:

```ts
import { useAuthStore } from '../stores/auth'

export default defineNuxtPlugin(async () => {
  const store = useAuthStore()

  // Runs during SSR; the result is serialised into the payload so the client
  // does not refetch on hydration.
  const { data } = await useFetch('/api/auth/me')

  store.setUser(data.value ?? null)
})
```

- [ ] **Step 6: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add auth store populated from the session cookie during SSR

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 11: Proactive refresh scheduling

Refresh must happen *before* the access token expires — `RevokeTokenRequestHandler` calls `ReadPrincipalFromExpiredToken`, which sets `ValidateLifetime = true` and therefore rejects an expired access token. Waiting for a 401 would be too late.

**Files:**
- Create: `lib/refreshSchedule.ts`
- Create: `app/plugins/refreshScheduler.client.ts`
- Test: `test/lib/refreshSchedule.spec.ts`

**Interfaces:**
- Produces: `msUntilRefresh(expSeconds: number, now?: number, leadSeconds?: number): number` — milliseconds to wait before refreshing; `0` when already inside the lead window.

- [ ] **Step 1: Write the failing test**

Create `test/lib/refreshSchedule.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { msUntilRefresh } from '../../lib/refreshSchedule'

const exp = 1_000 // expires at t = 1_000_000 ms

describe('msUntilRefresh', () => {
  it('schedules the refresh one lead-window before expiry', () => {
    // now = 0, lead = 300s -> refresh at 700s
    expect(msUntilRefresh(exp, 0, 300)).toBe(700_000)
  })

  it('returns 0 when already inside the lead window', () => {
    expect(msUntilRefresh(exp, 800_000, 300)).toBe(0)
  })

  it('returns 0 when already expired', () => {
    expect(msUntilRefresh(exp, 2_000_000, 300)).toBe(0)
  })

  it('defaults to a five minute lead', () => {
    expect(msUntilRefresh(exp, 0)).toBe(700_000)
  })
})
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/lib/refreshSchedule.spec.ts
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement**

Create `lib/refreshSchedule.ts`:

```ts
export const DEFAULT_LEAD_SECONDS = 300

export function msUntilRefresh(
  expSeconds: number,
  now = Date.now(),
  leadSeconds = DEFAULT_LEAD_SECONDS,
): number {
  const refreshAt = (expSeconds - leadSeconds) * 1000
  return Math.max(0, refreshAt - now)
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
pnpm test test/lib/refreshSchedule.spec.ts
```

Expected: all 4 pass.

- [ ] **Step 5: Wire the scheduler**

Create `app/plugins/refreshScheduler.client.ts`:

```ts
import { msUntilRefresh } from '../../lib/refreshSchedule'
import { useAuthStore } from '../stores/auth'

export default defineNuxtPlugin(() => {
  const store = useAuthStore()
  let timer: ReturnType<typeof setTimeout> | null = null

  async function scheduleNext(): Promise<void> {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }

    if (!store.isAuthenticated) {
      return
    }

    // /api/auth/me does not expose exp, so ask the server how long is left.
    const session = await $fetch<{ exp: number } | null>('/api/auth/session')
    if (!session) {
      store.setUser(null)
      return
    }

    timer = setTimeout(async () => {
      const ok = await store.refresh()
      if (ok) {
        await scheduleNext()
      }
    }, msUntilRefresh(session.exp))
  }

  watch(() => store.isAuthenticated, () => { void scheduleNext() }, { immediate: true })
})
```

- [ ] **Step 6: Add the session endpoint the scheduler needs**

Create `server/api/auth/session.get.ts`:

```ts
import { decodeJwtPayload, isExpired } from '../../../lib/jwt'
import { readAuthCookies } from '../../utils/authCookies'

export default defineEventHandler((event): { exp: number } | null => {
  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    return null
  }

  const payload = decodeJwtPayload(accessToken)
  if (!payload || isExpired(payload)) {
    return null
  }

  return { exp: payload.exp }
})
```

Only `exp` is returned — never the token itself.

- [ ] **Step 7: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): refresh tokens proactively before expiry

The API rejects refresh once the access token has expired, so reactive
refresh-on-401 cannot work here.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 12: Route middleware

**Files:**
- Create: `app/middleware/auth.ts`
- Create: `app/middleware/manage-films.ts`
- Create: `app/middleware/admin.ts`

**Interfaces:**
- Produces: route middleware named `auth`, `manage-films`, `admin`, applied via `definePageMeta({ middleware: 'admin' })`.

Middleware names match the capability they gate, mirroring the store getters: `manage-films` → `canManageFilms` (Administrator or Moderator), `admin` → `isAdministrator`.

- [ ] **Step 1: Implement the authenticated-user guard**

Create `app/middleware/auth.ts`:

```ts
import { useAuthStore } from '../stores/auth'

export default defineNuxtRouteMiddleware((to) => {
  const store = useAuthStore()

  if (!store.isAuthenticated) {
    return navigateTo({ path: '/login', query: { redirect: to.fullPath } })
  }
})
```

- [ ] **Step 2: Implement the film-management guard**

Create `app/middleware/manage-films.ts`:

```ts
import { useAuthStore } from '../stores/auth'

export default defineNuxtRouteMiddleware((to) => {
  const store = useAuthStore()

  if (!store.isAuthenticated) {
    return navigateTo({ path: '/login', query: { redirect: to.fullPath } })
  }

  if (!store.canManageFilms) {
    throw createError({ statusCode: 403, statusMessage: 'Forbidden' })
  }
})
```

- [ ] **Step 3: Implement the administrator guard**

Create `app/middleware/admin.ts`:

```ts
import { useAuthStore } from '../stores/auth'

export default defineNuxtRouteMiddleware((to) => {
  const store = useAuthStore()

  if (!store.isAuthenticated) {
    return navigateTo({ path: '/login', query: { redirect: to.fullPath } })
  }

  if (!store.isAdministrator) {
    throw createError({ statusCode: 403, statusMessage: 'Forbidden' })
  }
})
```

Unauthenticated users are redirected to log in; authenticated users lacking the role get a 403 rather than a redirect, because sending them to a login page they are already past is a dead end.

- [ ] **Step 4: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add auth and role route guards

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 13: Apollo client, dual-context

**Files:**
- Modify: `nuxt.config.ts`
- Create: `app/composables/useGraphql.ts`

**Interfaces:**
- Produces: `useGraphql<TResult, TVars>(document, variables): Promise<TResult>` — one call site for GraphQL, routed to the proxy in the browser and to the API directly during SSR.

- [ ] **Step 1: Install and register the Apollo module**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm add @nuxtjs/apollo @apollo/client graphql
```

Add `'@nuxtjs/apollo'` to `modules` in `nuxt.config.ts` and configure:

```ts
apollo: {
  clients: {
    default: {
      httpEndpoint: '/api/graphql',
    },
  },
},
```

The browser endpoint is the same-origin proxy. SSR is handled in Step 2.

- [ ] **Step 2: Add the single GraphQL entry point**

Create `app/composables/useGraphql.ts`:

```ts
import type { TypedDocumentNode } from '@apollo/client/core'
import { print } from 'graphql'

interface GraphqlEnvelope<T> {
  data?: T
  errors?: Array<{ message: string }>
}

/**
 * Both contexts go through the same-origin proxy, which owns the one place
 * the httpOnly cookie becomes a bearer header.
 *
 * In the browser that is a real request. During SSR, Nitro handles $fetch to
 * its own route in-process (no network hop), so the only extra work is
 * forwarding the incoming request's cookie header for the proxy to read.
 */
export async function useGraphql<TResult, TVars extends Record<string, unknown>>(
  document: TypedDocumentNode<TResult, TVars>,
  variables: TVars,
): Promise<TResult> {
  const query = print(document)

  const response = await $fetch<GraphqlEnvelope<TResult>>('/api/graphql', {
    method: 'POST',
    headers: import.meta.server ? useRequestHeaders(['cookie']) : undefined,
    body: { query, variables },
  })

  if (response.errors?.length) {
    throw createError({ statusCode: 400, statusMessage: response.errors[0].message })
  }

  return response.data as TResult
}
```

Routing SSR through the in-process proxy rather than calling the API directly keeps the
cookie-to-bearer translation in exactly one file, and avoids importing `server/` code from `app/` —
which is a boundary Nuxt does not guarantee is safe to cross.

- [ ] **Step 3: Wire up routing — without this, no page in this plan renders**

The scaffold left `app/app.vue` rendering `<NuxtWelcome />` with no `<NuxtPage />`, and no other task in this plan touches that file. When an `app.vue` exists and does not render `<NuxtPage />`, Nuxt bypasses file-based routing entirely: every page Tasks 13–18 create would be silently unreachable, and Task 14's `app/layouts/default.vue` would never apply without `<NuxtLayout>`.

Replace the entire contents of `app/app.vue` with:

```vue
<template>
  <UApp>
    <NuxtRouteAnnouncer />
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </UApp>
</template>
```

`<UApp>` is NuxtUI v4's documented root component — it hosts the portals toasts, modals and tooltips render into. Nothing in this plan uses those yet, but it is the documented wrapper and costs nothing here.

- [ ] **Step 4: Verify a query works in both contexts**

Temporarily replace `app/pages/index.vue` (create it if the scaffold used a different entry) with:

```vue
<script setup lang="ts">
import { GetFilmsDocument } from '../../graphql/generated/graphql'

const { data } = await useAsyncData('films-smoke', () =>
  useGraphql(GetFilmsDocument, { first: 3 }))
</script>

<template>
  <pre>{{ data }}</pre>
</template>
```

With the API running (see Task 9 Step 2), run `pnpm dev` and open `http://localhost:3000`. Expected: the film data renders in the server-sent HTML. Confirm by viewing source (Ctrl+U) — the film titles must be present in the raw HTML, proving SSR fetched them, not the browser.

- [ ] **Step 5: Fallback if `@nuxtjs/apollo` does not support the installed Nuxt major**

If Step 1 fails on a peer-dependency or runtime incompatibility, remove the module and keep `useGraphql` as the only GraphQL entry point — it does not depend on Apollo at runtime beyond `print()` and the `TypedDocumentNode` type:

```bash
pnpm remove @nuxtjs/apollo
```

Remove `'@nuxtjs/apollo'` from `modules` and delete the `apollo:` block from `nuxt.config.ts`. `@apollo/client` and `graphql` stay for the types and `print`. Everything else in this plan works unchanged, since no task imports Apollo's composables directly. Note the decision in the commit message.

- [ ] **Step 6: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add dual-context GraphQL access

SSR calls the API directly with the request's token; the browser goes
through the same-origin proxy.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 4 — Pages

### Task 14: Login and register pages

**Files:**
- Create: `app/pages/login.vue`
- Create: `app/pages/register.vue`
- Create: `app/layouts/default.vue`

**Interfaces:**
- Consumes: `useAuthStore().login/register` from Task 10.

NuxtUI component check: this task uses `UForm`, `UFormField`, `UInput`, `UButton`, `UCard`, `UAlert`. If the installed NuxtUI major names the field wrapper `UFormGroup` instead of `UFormField`, rename it throughout this task and Tasks 17–18.

- [ ] **Step 1: Create the shared layout**

Create `app/layouts/default.vue`:

```vue
<script setup lang="ts">
import { useAuthStore } from '../stores/auth'

const store = useAuthStore()

async function onLogout() {
  await store.logout()
  await navigateTo('/login')
}
</script>

<template>
  <div class="min-h-screen">
    <header class="border-b">
      <nav class="mx-auto flex max-w-5xl items-center justify-between p-4">
        <NuxtLink to="/films" class="font-semibold">
          Movie Database
        </NuxtLink>
        <div class="flex items-center gap-3">
          <template v-if="store.isAuthenticated">
            <span class="text-sm">{{ store.user?.username }}</span>
            <UButton variant="ghost" @click="onLogout">
              Log out
            </UButton>
          </template>
          <template v-else>
            <UButton to="/login" variant="ghost">
              Log in
            </UButton>
            <UButton to="/register">
              Register
            </UButton>
          </template>
        </div>
      </nav>
    </header>
    <main class="mx-auto max-w-5xl p-4">
      <slot />
    </main>
  </div>
</template>
```

- [ ] **Step 2: Create the login page**

Create `app/pages/login.vue`:

```vue
<script setup lang="ts">
import { z } from 'zod'
import { useAuthStore } from '../stores/auth'

const schema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(1, 'Enter your password'),
})

const state = reactive({ email: '', password: '' })
const error = ref<string | null>(null)
const pending = ref(false)

const store = useAuthStore()
const route = useRoute()

async function onSubmit() {
  error.value = null
  pending.value = true
  try {
    await store.login(state.email, state.password)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/films'
    await navigateTo(redirect)
  }
  catch {
    error.value = 'Those credentials were not accepted.'
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <UCard class="mx-auto max-w-md">
    <template #header>
      <h1 class="text-lg font-semibold">
        Log in
      </h1>
    </template>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
      <UFormField label="Email" name="email">
        <UInput v-model="state.email" type="email" autocomplete="email" />
      </UFormField>

      <UFormField label="Password" name="password">
        <UInput v-model="state.password" type="password" autocomplete="current-password" />
      </UFormField>

      <UButton type="submit" :loading="pending" block>
        Log in
      </UButton>
    </UForm>
  </UCard>
</template>
```

- [ ] **Step 3: Create the register page**

Create `app/pages/register.vue`:

```vue
<script setup lang="ts">
import { z } from 'zod'
import { useAuthStore } from '../stores/auth'

const schema = z.object({
  username: z.string().min(1, 'Choose a username'),
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(8, 'Use at least 8 characters'),
})

const state = reactive({ username: '', email: '', password: '' })
const error = ref<string | null>(null)
const pending = ref(false)

const store = useAuthStore()

async function onSubmit() {
  error.value = null
  pending.value = true
  try {
    await store.register(state.username, state.email, state.password)
    await navigateTo('/films')
  }
  catch {
    error.value = 'That account could not be created. The email may already be registered.'
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <UCard class="mx-auto max-w-md">
    <template #header>
      <h1 class="text-lg font-semibold">
        Create an account
      </h1>
    </template>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
      <UFormField label="Username" name="username">
        <UInput v-model="state.username" autocomplete="username" />
      </UFormField>

      <UFormField label="Email" name="email">
        <UInput v-model="state.email" type="email" autocomplete="email" />
      </UFormField>

      <UFormField label="Password" name="password">
        <UInput v-model="state.password" type="password" autocomplete="new-password" />
      </UFormField>

      <UButton type="submit" :loading="pending" block>
        Register
      </UButton>
    </UForm>
  </UCard>
</template>
```

The 8-character minimum is a frontend choice — the API's `CreateUserRequest` does not enforce a password policy.

- [ ] **Step 4: Verify manually**

With the API running, `pnpm dev`, then register a new account at `http://localhost:3000/register`. Expected: redirect to `/films`, and the header shows the username. Then log out and log back in at `/login`.

Verify the cookie is HttpOnly: open DevTools → Application → Cookies → `http://localhost:3000`. `md_access_token` must show a checked **HttpOnly** column. Confirm `document.cookie` in the console does **not** contain it.

- [ ] **Step 5: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add login and register pages

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 15: Films list page

**Files:**
- Create: `app/components/FilmCard.vue`
- Create: `app/pages/films/index.vue`
- Test: `test/components/FilmCard.spec.ts`

**Interfaces:**
- Produces: `<FilmCard :film="film" />` where `film` has `{ id, title, description, releaseDate, thumbnailUrl, director, genres }`.

- [ ] **Step 1: Write the failing component test**

Create `test/components/FilmCard.spec.ts`:

```ts
import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import FilmCard from '../../app/components/FilmCard.vue'

const film = {
  id: 'film-1',
  title: 'Arrival',
  description: 'Linguists meet heptapods.',
  releaseDate: '2016-11-11',
  thumbnailUrl: null,
  director: { id: 'd1', name: 'Denis', surname: 'Villeneuve' },
  genres: [{ id: 'g1', name: 'Science Fiction' }],
}

describe('FilmCard', () => {
  it('shows the title, director and genres', () => {
    render(FilmCard, { props: { film } })

    expect(screen.getByText('Arrival')).toBeTruthy()
    expect(screen.getByText(/Denis Villeneuve/)).toBeTruthy()
    expect(screen.getByText('Science Fiction')).toBeTruthy()
  })

  it('falls back to a placeholder when there is no thumbnail', () => {
    render(FilmCard, { props: { film } })

    expect(screen.queryByRole('img')).toBeNull()
  })

  it('renders the image when a thumbnail url is present', () => {
    render(FilmCard, { props: { film: { ...film, thumbnailUrl: 'https://cdn.test/content/film-1.jpg' } } })

    expect(screen.getByRole('img').getAttribute('src')).toBe('https://cdn.test/content/film-1.jpg')
  })
})
```

The no-thumbnail case is the one that matters in practice: `thumbnailUrl` is currently always `null`, because nothing can set `Film.Thumbnail` (see the spec's Out of scope).

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/components/FilmCard.spec.ts
```

Expected: FAIL — cannot resolve the component.

- [ ] **Step 3: Implement the card**

Create `app/components/FilmCard.vue`:

```vue
<script setup lang="ts">
interface Person { id: string, name: string, surname?: string | null }
interface Genre { id: string, name: string }

const props = defineProps<{
  film: {
    id: string
    title: string
    description?: string | null
    releaseDate: string
    thumbnailUrl?: string | null
    director?: Person | null
    genres?: Genre[] | null
  }
}>()

const directorName = computed(() => {
  const director = props.film.director
  return director ? `${director.name} ${director.surname ?? ''}`.trim() : null
})
</script>

<template>
  <UCard>
    <img
      v-if="film.thumbnailUrl"
      :src="film.thumbnailUrl"
      :alt="film.title"
      class="mb-3 aspect-video w-full rounded object-cover"
    >
    <div v-else class="mb-3 aspect-video w-full rounded bg-gray-100 dark:bg-gray-800" />

    <h2 class="font-semibold">
      <NuxtLink :to="`/films/${film.id}`">
        {{ film.title }}
      </NuxtLink>
    </h2>

    <p v-if="directorName" class="text-sm text-gray-500">
      Directed by {{ directorName }}
    </p>

    <p v-if="film.description" class="mt-2 line-clamp-3 text-sm">
      {{ film.description }}
    </p>

    <div v-if="film.genres?.length" class="mt-3 flex flex-wrap gap-1">
      <UBadge v-for="genre in film.genres" :key="genre.id" variant="subtle">
        {{ genre.name }}
      </UBadge>
    </div>
  </UCard>
</template>
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
pnpm test test/components/FilmCard.spec.ts
```

Expected: all 3 pass. If NuxtUI components are not resolvable in the test environment, add them as global stubs in `test/setup.ts`:

```ts
import { config } from '@vue/test-utils'

config.global.stubs = {
  UCard: { template: '<div><slot /></div>' },
  UBadge: { template: '<span><slot /></span>' },
  NuxtLink: { template: '<a><slot /></a>' },
}
```

- [ ] **Step 5: Implement the list page**

Create `app/pages/films/index.vue`:

```vue
<script setup lang="ts">
import { GetFilmsDocument } from '../../../graphql/generated/graphql'
import { useAuthStore } from '../../stores/auth'

const PAGE_SIZE = 12 // must stay at or below the API's MaxPageSize of 20

const store = useAuthStore()
const search = ref('')
const cursor = ref<string | null>(null)

const { data, pending, refresh } = await useAsyncData(
  'films',
  () => useGraphql(GetFilmsDocument, {
    first: PAGE_SIZE,
    after: cursor.value,
    where: search.value ? { title: { contains: search.value } } : null,
    order: [{ title: 'ASC' }],
  }),
  { watch: [cursor] },
)

const films = computed(() => data.value?.films?.nodes ?? [])
const pageInfo = computed(() => data.value?.films?.pageInfo)

let debounce: ReturnType<typeof setTimeout>
watch(search, () => {
  clearTimeout(debounce)
  debounce = setTimeout(() => {
    cursor.value = null
    void refresh()
  }, 300)
})
</script>

<template>
  <div>
    <div class="mb-4 flex items-center justify-between gap-4">
      <h1 class="text-xl font-semibold">
        Films
      </h1>
      <div class="flex items-center gap-2">
        <UInput v-model="search" placeholder="Search by title" />
        <UButton v-if="store.isAdministrator" to="/films/new">
          Add film
        </UButton>
      </div>
    </div>

    <div v-if="pending" class="py-8 text-center text-sm text-gray-500">
      Loading…
    </div>

    <div v-else-if="!films.length" class="py-8 text-center text-sm text-gray-500">
      No films found.
    </div>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <FilmCard v-for="film in films" :key="film.id" :film="film" />
    </div>

    <div v-if="pageInfo?.hasNextPage" class="mt-6 text-center">
      <UButton variant="outline" @click="cursor = pageInfo?.endCursor ?? null">
        Load more
      </UButton>
    </div>
  </div>
</template>
```

If codegen named the filter field differently (`title: { contains: ... }` depends on HotChocolate's generated `FilmFilterInput`), check `test/MovieDatabase.IntegrationTests/schema.graphql` for the exact operation name and correct it.

- [ ] **Step 6: Make the site root land on the film list**

Task 13 Step 4 left `app/pages/index.vue` as a temporary smoke-test page. Replace its entire contents with a redirect:

```vue
<script setup lang="ts">
await navigateTo('/films', { redirectCode: 302 })
</script>

<template>
  <div />
</template>
```

- [ ] **Step 7: Verify manually**

`pnpm dev`, open `http://localhost:3000`. Expected: redirect to `/films` with seeded films rendered. View source and confirm the titles appear in the server-sent HTML. Type in the search box and confirm the list narrows.

- [ ] **Step 8: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add films list page with search and paging

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 16: Film detail page

**Files:**
- Create: `app/pages/films/[id].vue`

**Interfaces:**
- Consumes: `GetFilmByIdDocument` from Task 4.

The API has no single-film resolver, so this filters the paginated field and takes the first node.

- [ ] **Step 1: Implement the page**

Create `app/pages/films/[id].vue`:

```vue
<script setup lang="ts">
import { GetFilmByIdDocument } from '../../../graphql/generated/graphql'
import { useAuthStore } from '../../stores/auth'

const route = useRoute()
const store = useAuthStore()
const id = route.params.id as string

const { data } = await useAsyncData(`film-${id}`, () =>
  useGraphql(GetFilmByIdDocument, { id }))

const film = computed(() => data.value?.films?.nodes?.[0] ?? null)

if (!film.value) {
  throw createError({ statusCode: 404, statusMessage: 'Film not found', fatal: true })
}

const people = computed(() => film.value?.actors ?? [])
</script>

<template>
  <article v-if="film">
    <div class="mb-4 flex items-start justify-between gap-4">
      <div>
        <h1 class="text-2xl font-semibold">
          {{ film.title }}
        </h1>
        <p class="text-sm text-gray-500">
          Released {{ film.releaseDate }}
        </p>
      </div>
      <div v-if="store.canManageFilms" class="flex gap-2">
        <UButton :to="`/films/${film.id}/edit`" variant="outline">
          Edit
        </UButton>
      </div>
    </div>

    <img
      v-if="film.thumbnailUrl"
      :src="film.thumbnailUrl"
      :alt="film.title"
      class="mb-6 w-full max-w-2xl rounded"
    >

    <p v-if="film.description" class="mb-6 max-w-2xl">
      {{ film.description }}
    </p>

    <dl class="grid gap-4 sm:grid-cols-2">
      <div v-if="film.director">
        <dt class="text-sm font-medium text-gray-500">
          Director
        </dt>
        <dd>{{ film.director.name }} {{ film.director.surname }}</dd>
      </div>

      <div v-if="film.producer">
        <dt class="text-sm font-medium text-gray-500">
          Producer
        </dt>
        <dd>{{ film.producer.name }}</dd>
      </div>

      <div v-if="people.length">
        <dt class="text-sm font-medium text-gray-500">
          Cast
        </dt>
        <dd>
          <ul>
            <li v-for="actor in people" :key="actor.id">
              {{ actor.name }} {{ actor.surname }}
            </li>
          </ul>
        </dd>
      </div>

      <div v-if="film.genres?.length">
        <dt class="text-sm font-medium text-gray-500">
          Genres
        </dt>
        <dd class="flex flex-wrap gap-1">
          <UBadge v-for="genre in film.genres" :key="genre.id" variant="subtle">
            {{ genre.name }}
          </UBadge>
        </dd>
      </div>
    </dl>
  </article>
</template>
```

- [ ] **Step 2: Verify manually**

Click a film from `/films`. Expected: full detail renders server-side. Then visit `/films/00000000-0000-0000-0000-000000000000`. Expected: a 404 page, not a blank one.

- [ ] **Step 3: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add film detail page

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 17: Create film page

**Files:**
- Create: `graphql/mutations/films.graphql`
- Create: `app/components/FilmForm.vue`
- Create: `app/pages/films/new.vue`

**Interfaces:**
- Produces: `<FilmForm :initial="film | null" :pending="boolean" @submit="(value: FilmFormValue) => void" />`
- Produces: `interface FilmFormValue { title: string; releaseDate: string; description: string; director: { name: string; surname: string }; producer: { name: string }; actors: Array<{ name: string; surname: string }>; genres: Array<{ name: string }> }`

`FilmForm` is shared by Task 18, which is why its value shape is defined here in full.

- [ ] **Step 1: Add the film mutation documents**

Create `graphql/mutations/films.graphql`:

```graphql
mutation CreateFilm($input: CreateFilmInput!) {
  createFilm(input: $input) {
    id
    title
  }
}

mutation EditFilm($input: EditFilmInput!) {
  editFilm(input: $input) {
    id
    title
  }
}

mutation DeleteFilm($filmId: String!) {
  deleteFilm(filmId: $filmId)
}
```

Then regenerate:

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm codegen
```

- [ ] **Step 2: Build the shared form**

Create `app/components/FilmForm.vue`:

```vue
<script setup lang="ts">
import { z } from 'zod'

export interface FilmFormValue {
  title: string
  releaseDate: string
  description: string
  director: { name: string, surname: string }
  producer: { name: string }
  actors: Array<{ name: string, surname: string }>
  genres: Array<{ name: string }>
}

const props = defineProps<{
  initial?: Partial<FilmFormValue> | null
  pending?: boolean
  submitLabel?: string
}>()

const emit = defineEmits<{ submit: [value: FilmFormValue] }>()

const schema = z.object({
  title: z.string().min(1, 'Title is required'),
  releaseDate: z.string().min(1, 'Release date is required'),
  description: z.string(),
  director: z.object({
    name: z.string().min(1, 'Director first name is required'),
    surname: z.string().min(1, 'Director surname is required'),
  }),
  producer: z.object({
    name: z.string().min(1, 'Producer is required'),
  }),
})

const state = reactive<FilmFormValue>({
  title: props.initial?.title ?? '',
  releaseDate: props.initial?.releaseDate ?? '',
  description: props.initial?.description ?? '',
  director: {
    name: props.initial?.director?.name ?? '',
    surname: props.initial?.director?.surname ?? '',
  },
  producer: { name: props.initial?.producer?.name ?? '' },
  actors: props.initial?.actors ? [...props.initial.actors] : [],
  genres: props.initial?.genres ? [...props.initial.genres] : [],
})

function addActor() {
  state.actors.push({ name: '', surname: '' })
}

function removeActor(index: number) {
  state.actors.splice(index, 1)
}

function addGenre() {
  state.genres.push({ name: '' })
}

function removeGenre(index: number) {
  state.genres.splice(index, 1)
}

function onSubmit() {
  emit('submit', {
    ...state,
    actors: state.actors.filter(actor => actor.name.trim() !== ''),
    genres: state.genres.filter(genre => genre.name.trim() !== ''),
  })
}
</script>

<template>
  <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
    <UFormField label="Title" name="title">
      <UInput v-model="state.title" />
    </UFormField>

    <UFormField label="Release date" name="releaseDate">
      <UInput v-model="state.releaseDate" type="date" />
    </UFormField>

    <UFormField label="Description" name="description">
      <UTextarea v-model="state.description" />
    </UFormField>

    <div class="grid gap-4 sm:grid-cols-2">
      <UFormField label="Director first name" name="director.name">
        <UInput v-model="state.director.name" />
      </UFormField>
      <UFormField label="Director surname" name="director.surname">
        <UInput v-model="state.director.surname" />
      </UFormField>
    </div>

    <UFormField label="Producer" name="producer.name">
      <UInput v-model="state.producer.name" />
    </UFormField>

    <div>
      <div class="mb-2 flex items-center justify-between">
        <span class="text-sm font-medium">Cast</span>
        <UButton size="xs" variant="ghost" @click="addActor">
          Add actor
        </UButton>
      </div>
      <div v-for="(actor, index) in state.actors" :key="index" class="mb-2 flex gap-2">
        <UInput v-model="actor.name" placeholder="First name" />
        <UInput v-model="actor.surname" placeholder="Surname" />
        <UButton size="xs" variant="ghost" color="error" @click="removeActor(index)">
          Remove
        </UButton>
      </div>
    </div>

    <div>
      <div class="mb-2 flex items-center justify-between">
        <span class="text-sm font-medium">Genres</span>
        <UButton size="xs" variant="ghost" @click="addGenre">
          Add genre
        </UButton>
      </div>
      <div v-for="(genre, index) in state.genres" :key="index" class="mb-2 flex gap-2">
        <UInput v-model="genre.name" placeholder="Genre" />
        <UButton size="xs" variant="ghost" color="error" @click="removeGenre(index)">
          Remove
        </UButton>
      </div>
    </div>

    <UButton type="submit" :loading="pending">
      {{ submitLabel ?? 'Save' }}
    </UButton>
  </UForm>
</template>
```

- [ ] **Step 3: Create the page**

Create `app/pages/films/new.vue`:

```vue
<script setup lang="ts">
import { CreateFilmDocument } from '../../../graphql/generated/graphql'
import type { FilmFormValue } from '../../components/FilmForm.vue'

definePageMeta({ middleware: 'admin' })

const pending = ref(false)
const error = ref<string | null>(null)

async function onSubmit(value: FilmFormValue) {
  pending.value = true
  error.value = null
  try {
    const result = await useGraphql(CreateFilmDocument, {
      input: {
        title: value.title,
        releaseDate: value.releaseDate,
        description: value.description,
        director: { name: value.director.name, surname: value.director.surname },
        producer: { name: value.producer.name },
        actors: value.actors,
        genres: value.genres,
      },
    })
    await navigateTo(`/films/${result.createFilm.id}`)
  }
  catch (caught) {
    error.value = caught instanceof Error && caught.message.includes('exist')
      ? 'A film with that title already exists.'
      : 'The film could not be created.'
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl">
    <h1 class="mb-4 text-xl font-semibold">
      Add film
    </h1>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <FilmForm :pending="pending" submit-label="Create film" @submit="onSubmit" />
  </div>
</template>
```

The duplicate-title case is handled explicitly because `CreateFilmRequestHandler` throws `FilmExistsApplicationException` for it, which is a user-correctable error rather than a system failure.

- [ ] **Step 4: Verify manually**

Log in as the seeded administrator (`admin@example.com` / `test`), open `/films/new`, create a film. Expected: redirect to its detail page. Then log in as a freshly registered user (role `User`) and open `/films/new`. Expected: a 403, and no "Add film" button on `/films`.

- [ ] **Step 5: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add administrator film creation page

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 18: Edit and delete film

**Files:**
- Create: `app/pages/films/[id]/edit.vue`
- Delete: `app/pages/films/[id].vue`
- Create: `app/pages/films/[id]/index.vue`

Nuxt cannot have both `films/[id].vue` and `films/[id]/edit.vue`. Move the detail page into the directory form.

**Interfaces:**
- Consumes: `FilmForm` and `FilmFormValue` from Task 17.

`editFilm` replaces every field it receives — `EditFilmRequestHandler` assigns `Title`, `Description`, `ReleaseDate`, `Director`, `Producer`, `Actors` and `Genres` unconditionally. The form must therefore submit the complete film, not a partial patch, or omitted fields are wiped.

- [ ] **Step 1: Move the detail page**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
mkdir -p app/pages/films/\[id\]
git mv app/pages/films/\[id\].vue app/pages/films/\[id\]/index.vue
```

Fix the now-shallower relative imports inside `app/pages/films/[id]/index.vue` by adding one more `../` level:

```ts
import { GetFilmByIdDocument } from '../../../../graphql/generated/graphql'
import { useAuthStore } from '../../../stores/auth'
```

- [ ] **Step 2: Verify the move did not break the detail page**

```bash
pnpm dev
```

Open a film's detail page. Expected: unchanged behaviour. Stop the server.

- [ ] **Step 3: Create the edit page**

Create `app/pages/films/[id]/edit.vue`:

```vue
<script setup lang="ts">
import { DeleteFilmDocument, EditFilmDocument, GetFilmByIdDocument } from '../../../../graphql/generated/graphql'
import type { FilmFormValue } from '../../../components/FilmForm.vue'
import { useAuthStore } from '../../../stores/auth'

definePageMeta({ middleware: 'manage-films' })

const route = useRoute()
const store = useAuthStore()
const id = route.params.id as string

const pending = ref(false)
const error = ref<string | null>(null)

const { data } = await useAsyncData(`film-edit-${id}`, () =>
  useGraphql(GetFilmByIdDocument, { id }))

const film = computed(() => data.value?.films?.nodes?.[0] ?? null)

if (!film.value) {
  throw createError({ statusCode: 404, statusMessage: 'Film not found', fatal: true })
}

const initial = computed<Partial<FilmFormValue>>(() => ({
  title: film.value?.title ?? '',
  releaseDate: film.value?.releaseDate ?? '',
  description: film.value?.description ?? '',
  director: {
    name: film.value?.director?.name ?? '',
    surname: film.value?.director?.surname ?? '',
  },
  producer: { name: film.value?.producer?.name ?? '' },
  actors: (film.value?.actors ?? []).map(actor => ({ name: actor.name, surname: actor.surname ?? '' })),
  genres: (film.value?.genres ?? []).map(genre => ({ name: genre.name })),
}))

async function onSubmit(value: FilmFormValue) {
  pending.value = true
  error.value = null
  try {
    // editFilm overwrites every field, so send the whole film.
    await useGraphql(EditFilmDocument, {
      input: {
        id,
        title: value.title,
        releaseDate: value.releaseDate,
        description: value.description,
        director: { name: value.director.name, surname: value.director.surname },
        producer: { name: value.producer.name },
        actors: value.actors,
        genres: value.genres,
      },
    })
    await navigateTo(`/films/${id}`)
  }
  catch {
    error.value = 'The film could not be saved.'
  }
  finally {
    pending.value = false
  }
}

async function onDelete() {
  if (!confirm('Delete this film? This cannot be undone.')) {
    return
  }

  pending.value = true
  error.value = null
  try {
    await useGraphql(DeleteFilmDocument, { filmId: id })
    await navigateTo('/films')
  }
  catch {
    error.value = 'The film could not be deleted.'
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl">
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">
        Edit film
      </h1>
      <UButton v-if="store.isAdministrator" color="error" variant="outline" :loading="pending" @click="onDelete">
        Delete
      </UButton>
    </div>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <FilmForm :initial="initial" :pending="pending" submit-label="Save changes" @submit="onSubmit" />
  </div>
</template>
```

Delete is Administrator-only (`deleteFilm` carries `[Authorize(Roles = [Administrator])]`) even though the page itself is open to Moderators, so the button is gated separately from the route.

- [ ] **Step 4: Verify manually**

As the administrator, edit a film and confirm the changes persist. Confirm the cast and genres survive a save — if they vanish, the form is sending a partial input. Then delete a test film and confirm it disappears from `/films`.

- [ ] **Step 5: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add -A src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add film editing and deletion

editFilm overwrites every field, so the form submits the complete film.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 5 — Upload and orchestration

### Task 19: Image upload

`uploadBlob` uses the GraphQL multipart request specification, so the JSON proxy from Task 9 cannot carry it. This adds a dedicated multipart route.

The uploaded image cannot yet be attached to a film — `Film.Thumbnail` has no mutation write path. This task delivers the upload capability and a reusable component; wiring it to a film is future backend work.

**Files:**
- Create: `server/api/upload.post.ts`
- Create: `app/components/ImageUploader.vue`
- Test: `test/lib/uploadValidation.spec.ts`
- Create: `lib/uploadValidation.ts`

**Interfaces:**
- Produces: `validateUpload(file: { type: string; size: number }): string | null` — returns an error message, or `null` when acceptable.
- Produces: `POST /api/upload` (multipart, field name `file`) → `{ id, fileName, url }`
- Produces: `<ImageUploader @uploaded="(blob: { id: string; url: string }) => void" />`

- [ ] **Step 1: Write the failing validation test**

Create `test/lib/uploadValidation.spec.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { MAX_UPLOAD_BYTES, validateUpload } from '../../lib/uploadValidation'

describe('validateUpload', () => {
  it('accepts every content type the API allows', () => {
    for (const type of ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']) {
      expect(validateUpload({ type, size: 1_000 })).toBeNull()
    }
  })

  it('rejects a disallowed content type', () => {
    expect(validateUpload({ type: 'image/gif', size: 1_000 })).toMatch(/PNG|JPEG|WebP/i)
  })

  it('rejects a file at or above the size limit', () => {
    expect(validateUpload({ type: 'image/png', size: MAX_UPLOAD_BYTES })).toMatch(/5 MB/)
  })

  it('accepts a file just under the limit', () => {
    expect(validateUpload({ type: 'image/png', size: MAX_UPLOAD_BYTES - 1 })).toBeNull()
  })
})
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test test/lib/uploadValidation.spec.ts
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement validation**

Create `lib/uploadValidation.ts`:

```ts
/** Mirrors Constants.Blob.MaxBlobSizeInBytes. */
export const MAX_UPLOAD_BYTES = 5_000_000

/** Mirrors Constants.Blob.AllowedContentTypes. */
export const ALLOWED_CONTENT_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']

export function validateUpload(file: { type: string, size: number }): string | null {
  if (!ALLOWED_CONTENT_TYPES.includes(file.type)) {
    return 'Choose a PNG, JPEG or WebP image.'
  }

  if (file.size >= MAX_UPLOAD_BYTES) {
    return 'Choose an image smaller than 5 MB.'
  }

  return null
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
pnpm test test/lib/uploadValidation.spec.ts
```

Expected: all 4 pass.

- [ ] **Step 5: Implement the multipart proxy**

Create `server/api/upload.post.ts`:

```ts
import { createError } from 'h3'
import { readAuthCookies } from '../utils/authCookies'

const UPLOAD_BLOB_MUTATION = `
  mutation UploadBlob($file: Upload!) {
    uploadBlob(file: $file) {
      id
      fileName
      url
    }
  }
`

export default defineEventHandler(async (event) => {
  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    throw createError({ statusCode: 401, statusMessage: 'Not authenticated' })
  }

  const parts = await readMultipartFormData(event)
  const filePart = parts?.find(part => part.name === 'file')

  if (!filePart?.data) {
    throw createError({ statusCode: 400, statusMessage: 'No file provided' })
  }

  // GraphQL multipart request spec: operations + map + the file itself.
  const form = new FormData()
  form.append('operations', JSON.stringify({
    query: UPLOAD_BLOB_MUTATION,
    variables: { file: null },
  }))
  form.append('map', JSON.stringify({ 0: ['variables.file'] }))
  form.append('0', new Blob([filePart.data], { type: filePart.type ?? 'application/octet-stream' }), filePart.filename ?? 'upload')

  const endpoint = useRuntimeConfig(event).graphqlEndpoint

  const response = await $fetch<{ data?: { uploadBlob: { id: string, fileName: string, url: string } }, errors?: Array<{ message: string }> }>(
    endpoint,
    {
      method: 'POST',
      headers: {
        authorization: `Bearer ${accessToken}`,
        'GraphQL-Preflight': '1',
      },
      body: form,
    },
  )

  if (response.errors?.length) {
    throw createError({ statusCode: 400, statusMessage: response.errors[0].message })
  }

  return response.data!.uploadBlob
})
```

The `GraphQL-Preflight` header is HotChocolate's CSRF protection for multipart requests. If the upload is rejected with a preflight or CSRF error, check the server's response body — HotChocolate names the required header in the error, and it has changed between majors.

- [ ] **Step 6: Build the component**

Create `app/components/ImageUploader.vue`:

```vue
<script setup lang="ts">
import { validateUpload } from '../../lib/uploadValidation'

const emit = defineEmits<{ uploaded: [blob: { id: string, fileName: string, url: string }] }>()

const error = ref<string | null>(null)
const pending = ref(false)

async function onChange(payload: Event) {
  const input = payload.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) {
    return
  }

  error.value = validateUpload(file)
  if (error.value) {
    input.value = ''
    return
  }

  pending.value = true
  try {
    const form = new FormData()
    form.append('file', file)
    const blob = await $fetch<{ id: string, fileName: string, url: string }>('/api/upload', {
      method: 'POST',
      body: form,
    })
    emit('uploaded', blob)
  }
  catch {
    error.value = 'The image could not be uploaded.'
  }
  finally {
    pending.value = false
    input.value = ''
  }
}
</script>

<template>
  <div>
    <input
      type="file"
      accept="image/png,image/jpeg,image/webp"
      :disabled="pending"
      @change="onChange"
    >
    <p v-if="pending" class="mt-1 text-sm text-gray-500">
      Uploading…
    </p>
    <p v-if="error" class="mt-1 text-sm text-red-600">
      {{ error }}
    </p>
  </div>
</template>
```

The component is not mounted on any page yet — there is nothing to attach the result to. It is built and tested so that the write path, when it exists, is a small change.

- [ ] **Step 7: Verify the upload end to end**

Temporarily add `<ImageUploader @uploaded="blob => console.log(blob)" />` to `app/pages/films/index.vue`, log in, and upload a small PNG. Expected: a `{ id, fileName, url }` object in the console. Then check the Aspire dashboard's Functions logs for `ImageBlobTrigger` firing and a `Thumbnail created for blob` line — proving the whole pipeline (upload → images container → compression → content container) works. Remove the temporary markup afterwards.

- [ ] **Step 8: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add src/Frontend/MovieDatabase.Web
git commit -m "$(cat <<'EOF'
feat(web): add image upload with multipart proxy

The uploader is not yet mounted on a page: Film.Thumbnail has no mutation
write path, so there is nothing to attach the result to.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 20: Aspire orchestration

**Files:**
- Modify: `src/Boostrapper/MovieDatabase.AppHost/AppHost.cs`
- Modify: `src/Boostrapper/MovieDatabase.AppHost/MovieDatabase.AppHost.csproj`
- Modify: `src/Shared/MovieDatabase.SharedKernel/Configurations/ApiConfiguration.cs` (if a resource-name constant is added)
- Modify: `README.md`

**Interfaces:**
- Produces: a `web` resource in the Aspire graph that starts the Nuxt dev server with `NUXT_GRAPHQL_ENDPOINT` pointing at the API.

- [ ] **Step 1: Use the aspireify skill for the exact wiring**

The Aspire Node-app hosting API's exact shape varies by version, so do not hand-write this from memory. Invoke the `aspire:aspireify` skill with this goal:

> Add the existing Nuxt app at `src/Frontend/MovieDatabase.Web` to the AppHost as a Node/npm application resource using pnpm, running the `dev` script. It must reference the existing API project resource so the API's HTTPS URL is injected into the Nuxt process as the environment variable `NUXT_GRAPHQL_ENDPOINT`, with `/graphql` appended. It should expose an external HTTP endpoint and wait for the API.

The existing AppHost resources it must integrate with are in `src/Boostrapper/MovieDatabase.AppHost/AppHost.cs`: `cosmos`, `storage`, `blobs`, `functions`, and the API project registered as `ApiConfiguration.ModuleName`.

- [ ] **Step 2: Add the resource name as a shared constant**

Follow the existing convention — resource names live in `src/Shared/MovieDatabase.SharedKernel/Configurations/`, not as inline literals. Add to `ApiConfiguration.cs` or a new `WebConfiguration.cs`:

```csharp
namespace MovieDatabase.SharedKernel.Configurations;

public static class WebConfiguration
{
    public const string ModuleName = "web";
}
```

Use that constant in `AppHost.cs`.

- [ ] **Step 3: Verify the whole stack starts together**

```bash
cd "$(git rev-parse --show-toplevel)"
dotnet run --project src/Boostrapper/MovieDatabase.AppHost/MovieDatabase.AppHost.csproj
```

Expected: the Aspire dashboard lists `web` alongside cosmos, storage, functions and the API, all healthy. Open the `web` endpoint from the dashboard. Expected: the films list renders, with no `.env` file present — proving the endpoint arrived from Aspire rather than local config.

Delete any local `.env` first, so a stale value cannot mask a wiring failure.

- [ ] **Step 4: Document how to run it**

Add to `README.md` under **Running the Application**:

```markdown
### Frontend

The Nuxt frontend lives in `src/Frontend/MovieDatabase.Web` and starts automatically with the
AppHost — open the `web` resource from the Aspire dashboard.

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
```

- [ ] **Step 5: Run the full test suite**

```bash
cd "$(git rev-parse --show-toplevel)/src/Frontend/MovieDatabase.Web"
pnpm test
```

Expected: all tests pass.

```bash
cd "$(git rev-parse --show-toplevel)"
dotnet test test/MovieDatabase.UnitTests/MovieDatabase.UnitTests.csproj
```

Expected: 94 passed — unchanged by this plan, confirming the schema-tool fix in Task 1 broke nothing.

- [ ] **Step 6: Commit**

```bash
cd "$(git rev-parse --show-toplevel)"
git add -A
git commit -m "$(cat <<'EOF'
feat(apphost): orchestrate the Nuxt frontend with the rest of the stack

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Done

At this point the frontend supports public film browsing with search and paging, registration and login with HttpOnly-cookie sessions and proactive refresh, role-gated film creation, editing and deletion, and standalone image upload — all starting from a single `dotnet run` on the AppHost.

**Known follow-up work, deliberately not in this plan:**

- Attaching an uploaded image to a film requires a backend write path for `Film.Thumbnail`. Until then `thumbnailUrl` is always `null` and `ImageUploader` is unmounted.
- `ReadPrincipalFromExpiredToken` validates lifetime, which forces the proactive-refresh design in Task 11. If that is changed to `ValidateLifetime = false`, Task 11 can be replaced by simple refresh-on-401.
- Revocation is recorded but not enforced at authentication time, so logout cannot invalidate an already-issued access token.
