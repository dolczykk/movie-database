import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export interface AuthUser {
  id: string
  username: string
  email: string
  role: string | null
}

/**
 * Outcome of `refresh()`.
 *
 * - `success` — the session was renewed; `user` reflects the latest server state.
 * - `unauthenticated` — the API definitively rejected the token pair (401) and
 *   has already cleared the cookies. The session really is over: `user` is
 *   cleared and the caller should send the user to log in again.
 * - `retry` — an indeterminate failure (network blip, 503, ...). The cookies
 *   were left untouched because the session is probably still valid, so
 *   `user` is left untouched too. The caller should try again later.
 */
export type RefreshOutcome = 'success' | 'unauthenticated' | 'retry'

function isUnauthorized(error: unknown): boolean {
  return typeof error === 'object' && error !== null
    && 'statusCode' in error && (error as { statusCode?: unknown }).statusCode === 401
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

  async function refresh(): Promise<RefreshOutcome> {
    try {
      user.value = await $fetch<AuthUser>('/api/auth/refresh', { method: 'POST' })
      return 'success'
    }
    catch (error) {
      if (isUnauthorized(error)) {
        user.value = null
        return 'unauthenticated'
      }

      return 'retry'
    }
  }

  return { user, isAuthenticated, isAdministrator, canManageFilms, setUser, login, register, logout, refresh }
})
