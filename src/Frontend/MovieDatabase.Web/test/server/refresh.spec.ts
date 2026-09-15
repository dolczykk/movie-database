import { beforeEach, describe, expect, it, vi } from 'vitest'

const setCookie = vi.fn()
const getCookie = vi.fn()
const deleteCookie = vi.fn()

vi.mock('h3', async (importOriginal) => {
  const actual = await importOriginal<typeof import('h3')>()
  return { ...actual, setCookie, getCookie, deleteCookie }
})

const callGraphql = vi.fn()
vi.mock('../../server/utils/graphqlClient', () => ({ callGraphql }))

vi.stubGlobal('defineEventHandler', (handler: unknown) => handler)

const { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } = await import('../../server/utils/authCookies')
const { default: refreshHandler, isDefinitiveRejection } = await import('../../server/api/auth/refresh.post')

const handler = refreshHandler as unknown as (event: unknown) => Promise<unknown>
const event = {} as never

function stubCookies(accessToken: string, refreshToken: string) {
  getCookie.mockImplementation((_event: unknown, name: string) => {
    if (name === ACCESS_TOKEN_COOKIE) return accessToken
    if (name === REFRESH_TOKEN_COOKIE) return refreshToken
    return undefined
  })
}

beforeEach(() => {
  setCookie.mockClear()
  getCookie.mockClear()
  deleteCookie.mockClear()
  callGraphql.mockClear()
})

describe('isDefinitiveRejection', () => {
  it('is true for a 400-shaped error', () => {
    expect(isDefinitiveRejection({ statusCode: 400 })).toBe(true)
  })

  it('is false for a non-400 error', () => {
    expect(isDefinitiveRejection({ statusCode: 502 })).toBe(false)
    expect(isDefinitiveRejection(new Error('network error'))).toBe(false)
    expect(isDefinitiveRejection('boom')).toBe(false)
  })
})

describe('POST /api/auth/refresh', () => {
  it('clears cookies and returns 401 when the API definitively rejects the pair', async () => {
    stubCookies('access-token', 'refresh-token-400')
    callGraphql.mockRejectedValue(Object.assign(new Error('rejected'), { statusCode: 400, statusMessage: 'Refresh token invalid' }))

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 401, statusMessage: 'Session expired' })

    expect(deleteCookie).toHaveBeenCalled()
    expect(deleteCookie.mock.calls.map(call => call[1]))
      .toEqual(expect.arrayContaining([ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE]))
  })

  it('does not clear cookies and returns 503 on a network-style failure', async () => {
    stubCookies('access-token', 'refresh-token-network')
    callGraphql.mockRejectedValue(new TypeError('fetch failed'))

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 503, statusMessage: 'Unable to refresh session' })

    expect(deleteCookie).not.toHaveBeenCalled()
  })

  it('does not clear cookies and returns 503 when callGraphql surfaces a 502', async () => {
    stubCookies('access-token', 'refresh-token-502')
    callGraphql.mockRejectedValue(Object.assign(new Error('Empty GraphQL response'), { statusCode: 502 }))

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 503, statusMessage: 'Unable to refresh session' })

    expect(deleteCookie).not.toHaveBeenCalled()
  })
})
