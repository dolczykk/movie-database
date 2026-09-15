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
