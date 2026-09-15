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
