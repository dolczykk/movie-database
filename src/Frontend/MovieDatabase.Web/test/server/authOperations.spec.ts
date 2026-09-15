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
