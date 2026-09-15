import { beforeEach, describe, expect, it, vi } from 'vitest'

const readAuthCookies = vi.fn()
vi.mock('../../server/utils/authCookies', () => ({ readAuthCookies }))

vi.stubGlobal('defineEventHandler', (handler: unknown) => handler)

const setResponseHeader = vi.fn()
vi.stubGlobal('setResponseHeader', setResponseHeader)

const { default: sessionHandler } = await import('../../server/api/auth/session.get')

const handler = sessionHandler as unknown as (event: unknown) => unknown
const event = {} as never

function fakeToken(payload: Record<string, unknown>): string {
  const base64url = (value: string) => btoa(value).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${base64url('{}')}.${base64url(JSON.stringify(payload))}.signature`
}

beforeEach(() => {
  readAuthCookies.mockReset()
  setResponseHeader.mockReset()
})

describe('GET /api/auth/session', () => {
  it('sets Cache-Control: no-store, regardless of authentication state', () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })

    handler(event)

    expect(setResponseHeader).toHaveBeenCalledWith(event, 'cache-control', 'no-store')
  })

  it('returns null when there is no access token', () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })

    expect(handler(event)).toBeNull()
  })

  it('returns null for an expired token', () => {
    const token = fakeToken({ jti: '1', sub: 'alice', email: 'alice@example.com', role: null, exp: 0 })
    readAuthCookies.mockReturnValue({ accessToken: token, refreshToken: 'r' })

    expect(handler(event)).toBeNull()
  })

  it('returns the token expiry for a valid, unexpired token', () => {
    const farFuture = Math.floor(Date.now() / 1000) + 3600
    const token = fakeToken({ jti: '1', sub: 'alice', email: 'alice@example.com', role: null, exp: farFuture })
    readAuthCookies.mockReturnValue({ accessToken: token, refreshToken: 'r' })

    expect(handler(event)).toEqual({ exp: farFuture })
  })
})
