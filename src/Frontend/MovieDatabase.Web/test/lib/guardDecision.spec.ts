import { describe, expect, it } from 'vitest'
import { guardDecision } from '../../lib/guardDecision'

// Mirrors the four visitor states the middleware adapters see via the auth
// store, and how each of the three guards' capabilities is derived from a
// role (`isAuthenticated`, `canManageFilms`, `isAdministrator`).
type Role = 'Administrator' | 'Moderator' | 'User' | 'unauthenticated'

const roles: Role[] = ['Administrator', 'Moderator', 'User', 'unauthenticated']

function isAuthenticated(role: Role): boolean {
  return role !== 'unauthenticated'
}

function isAdministrator(role: Role): boolean {
  return role === 'Administrator'
}

function canManageFilms(role: Role): boolean {
  return role === 'Administrator' || role === 'Moderator'
}

describe('guardDecision', () => {
  describe('auth guard (any authenticated user)', () => {
    const expected: Record<Role, string> = {
      Administrator: 'allow',
      Moderator: 'allow',
      User: 'allow',
      unauthenticated: 'redirect',
    }

    it.each(roles)('%s', (role) => {
      const decision = guardDecision(isAuthenticated(role), isAuthenticated(role), '/uploads/new')
      expect(decision.type).toBe(expected[role])
    })
  })

  describe('manage-films guard (Administrator or Moderator)', () => {
    const expected: Record<Role, string> = {
      Administrator: 'allow',
      Moderator: 'allow',
      User: 'forbid',
      unauthenticated: 'redirect',
    }

    it.each(roles)('%s', (role) => {
      const decision = guardDecision(isAuthenticated(role), canManageFilms(role), '/films/1/edit')
      expect(decision.type).toBe(expected[role])
    })
  })

  describe('admin guard (Administrator only)', () => {
    const expected: Record<Role, string> = {
      Administrator: 'allow',
      Moderator: 'forbid',
      User: 'forbid',
      unauthenticated: 'redirect',
    }

    it.each(roles)('%s', (role) => {
      const decision = guardDecision(isAuthenticated(role), isAdministrator(role), '/films/create')
      expect(decision.type).toBe(expected[role])
    })
  })

  it('carries the original path in the redirect outcome', () => {
    const decision = guardDecision(false, false, '/films/42/edit')

    expect(decision).toEqual({
      type: 'redirect',
      to: { path: '/login', query: { redirect: '/films/42/edit' } },
    })
  })

  it('forbids, rather than redirects, an authenticated visitor lacking the capability', () => {
    const decision = guardDecision(true, false, '/films/create')

    expect(decision).toEqual({ type: 'forbid' })
  })

  it('allows an authenticated visitor holding the capability', () => {
    const decision = guardDecision(true, true, '/films/create')

    expect(decision).toEqual({ type: 'allow' })
  })

  it('redirects an unauthenticated visitor even when hasCapability is true', () => {
    // Authentication is checked first: an unauthenticated visitor always goes
    // to login, never straight to a 403, regardless of the capability flag.
    const decision = guardDecision(false, true, '/films/create')

    expect(decision.type).toBe('redirect')
  })
})
