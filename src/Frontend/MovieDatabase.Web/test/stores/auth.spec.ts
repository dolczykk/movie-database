import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const fetchMock = vi.fn()
vi.stubGlobal('$fetch', fetchMock)

const { useAuthStore } = await import('../../app/stores/auth')

const administrator = { id: '1', username: 'admin', email: 'a@example.com', role: 'Administrator' }
const moderator = { id: '2', username: 'mod', email: 'm@example.com', role: 'Moderator' }
const plainUser = { id: '3', username: 'user', email: 'u@example.com', role: 'User' }

beforeEach(() => {
  setActivePinia(createPinia())
  fetchMock.mockClear()
})

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

  describe('login', () => {
    it('sets the user returned by the API', async () => {
      fetchMock.mockResolvedValueOnce(administrator)
      const store = useAuthStore()

      await store.login('a@example.com', 'password')

      expect(store.user).toEqual(administrator)
      expect(fetchMock).toHaveBeenCalledWith('/api/auth/login', {
        method: 'POST',
        body: { email: 'a@example.com', password: 'password' },
      })
    })
  })

  describe('register', () => {
    it('sets the user returned by the API', async () => {
      fetchMock.mockResolvedValueOnce(plainUser)
      const store = useAuthStore()

      await store.register('user', 'u@example.com', 'password')

      expect(store.user).toEqual(plainUser)
      expect(fetchMock).toHaveBeenCalledWith('/api/auth/register', {
        method: 'POST',
        body: { username: 'user', email: 'u@example.com', password: 'password' },
      })
    })
  })

  describe('logout', () => {
    it('clears the user after the API call succeeds', async () => {
      fetchMock.mockResolvedValueOnce(undefined)
      const store = useAuthStore()
      store.setUser(administrator)

      await store.logout()

      expect(store.user).toBeNull()
      expect(fetchMock).toHaveBeenCalledWith('/api/auth/logout', { method: 'POST' })
    })
  })

  describe('refresh', () => {
    it('sets the user and reports success when the API returns a fresh user', async () => {
      fetchMock.mockResolvedValueOnce(administrator)
      const store = useAuthStore()

      const outcome = await store.refresh()

      expect(outcome).toBe('success')
      expect(store.user).toEqual(administrator)
      expect(fetchMock).toHaveBeenCalledWith('/api/auth/refresh', { method: 'POST' })
    })

    it('clears the user and reports unauthenticated on a 401 (the API already cleared the cookies)', async () => {
      const store = useAuthStore()
      store.setUser(administrator)
      fetchMock.mockRejectedValueOnce(Object.assign(new Error('Session expired'), { statusCode: 401 }))

      const outcome = await store.refresh()

      expect(outcome).toBe('unauthenticated')
      expect(store.user).toBeNull()
    })

    it('leaves the user untouched and reports retry on a 503 (indeterminate failure)', async () => {
      const store = useAuthStore()
      store.setUser(administrator)
      fetchMock.mockRejectedValueOnce(Object.assign(new Error('Unable to refresh session'), { statusCode: 503 }))

      const outcome = await store.refresh()

      expect(outcome).toBe('retry')
      expect(store.user).toEqual(administrator)
    })

    it('leaves the user untouched and reports retry on a network error with no status', async () => {
      const store = useAuthStore()
      store.setUser(moderator)
      fetchMock.mockRejectedValueOnce(new TypeError('fetch failed'))

      const outcome = await store.refresh()

      expect(outcome).toBe('retry')
      expect(store.user).toEqual(moderator)
    })
  })
})
