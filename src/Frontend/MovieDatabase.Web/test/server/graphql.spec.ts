import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// NOTE ON COVERAGE: this route also relies on Nitro's real auto-imports —
// `defineEventHandler`/`readBody` (bare globals here) and, inside
// `postGraphqlRaw` (server/utils/graphqlClient.ts), `useRuntimeConfig`/
// `$fetch` (real Nitro server imports, from `nitropack/runtime/internal/config`
// and a Nitro-assigned `globalThis.$fetch` respectively — NOT the client-side
// `#app/nuxt` / `#build/fetch.mjs` versions). This project's Vitest harness
// runs under plain `happy-dom`, not Nuxt's `environment: 'nuxt'`, so it does
// not exercise Nitro's real server-side auto-import resolution. That gap is
// covered by the deferred end-to-end verification (Docker/Aspire stack), not
// by this file. What this file DOES cover, by mocking the project-owned
// `postGraphqlRaw` seam (the same way `test/server/refresh.spec.ts` mocks
// `callGraphql`), is this route's own logic: threading the access-token
// cookie through, passing the GraphQL envelope back untouched, and turning a
// transport-level failure into a generic error that doesn't leak the
// upstream endpoint or response body.

const readAuthCookies = vi.fn()
vi.mock('../../server/utils/authCookies', () => ({ readAuthCookies }))

const postGraphqlRaw = vi.fn()
vi.mock('../../server/utils/graphqlClient', () => ({ postGraphqlRaw }))

vi.stubGlobal('defineEventHandler', (handler: unknown) => handler)

const { default: graphqlHandler } = await import('../../server/api/graphql.post')

const handler = graphqlHandler as unknown as (event: unknown) => Promise<unknown>
const event = {} as never

const readBody = vi.fn()
let consoleErrorSpy: ReturnType<typeof vi.spyOn>

beforeEach(() => {
  readAuthCookies.mockReset()
  postGraphqlRaw.mockReset()
  readBody.mockReset()

  vi.stubGlobal('readBody', readBody)

  readBody.mockResolvedValue({ query: '{ films { nodes { id } } }' })
  consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.unstubAllGlobals()
  consoleErrorSpy.mockRestore()
})

describe('POST /api/graphql', () => {
  it('forwards the access token from the cookie to the upstream request', async () => {
    readAuthCookies.mockReturnValue({ accessToken: 'access-token-value', refreshToken: 'refresh-token-value' })
    const body = { query: '{ films { nodes { id } } }' }
    readBody.mockResolvedValue(body)
    postGraphqlRaw.mockResolvedValue({ data: { films: { nodes: [] } } })

    await handler(event)

    expect(postGraphqlRaw).toHaveBeenCalledTimes(1)
    expect(postGraphqlRaw).toHaveBeenCalledWith(event, body, 'access-token-value')
  })

  it('forwards no access token when the cookie is absent', async () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })
    postGraphqlRaw.mockResolvedValue({ data: { films: { nodes: [] } } })

    await handler(event)

    const [, , accessToken] = postGraphqlRaw.mock.calls[0]
    expect(accessToken).toBeNull()
  })

  it('returns a successful GraphQL envelope untouched', async () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })
    const envelope = { data: { films: { nodes: [{ id: '1', title: 'Foo' }] } } }
    postGraphqlRaw.mockResolvedValue(envelope)

    await expect(handler(event)).resolves.toEqual(envelope)
  })

  it('returns a GraphQL error envelope untouched, without throwing (a successful HTTP response is never caught)', async () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })
    const envelope = { errors: [{ message: 'Cannot query field "invalid" on type "Query".' }] }
    postGraphqlRaw.mockResolvedValue(envelope)

    await expect(handler(event)).resolves.toEqual(envelope)
  })

  it('returns a generic error and does not leak the upstream endpoint or response body when the upstream request fails', async () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })
    const upstreamError = Object.assign(
      new Error('[POST] "https://internal-movie-api.internal:7204/graphql": 500 Internal Server Error'),
      { statusCode: 500, data: { secret: 'upstream response body' } },
    )
    postGraphqlRaw.mockRejectedValue(upstreamError)

    const rejection = await handler(event).catch((error: unknown) => error) as Record<string, unknown>

    expect(rejection).toMatchObject({ statusCode: 502, statusMessage: 'Unable to reach the GraphQL API' })
    expect(rejection.data).toBeUndefined()
    expect(JSON.stringify(rejection)).not.toContain('internal-movie-api')
    expect(consoleErrorSpy).toHaveBeenCalledWith('[api/graphql] upstream request failed', upstreamError.message)
  })
})
