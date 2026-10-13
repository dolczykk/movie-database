import type { AuthUser } from '../stores/auth'
import { useAuthStore } from '../stores/auth'

export default defineNuxtPlugin(async () => {
  const store = useAuthStore()

  // Runs during SSR; the result is serialised into the payload so the client
  // does not refetch on hydration.
  const { data } = await useFetch<{ user: AuthUser | null }>('/api/auth/me')

  store.setUser(data.value?.user ?? null)
})
