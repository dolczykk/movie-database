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
