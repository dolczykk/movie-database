import { isSuperseded, msUntilRefresh, nextSchedulerAction, RETRY_DELAY_MS } from '../../lib/refreshSchedule'
import { useAuthStore } from '../stores/auth'

/**
 * Refreshes the access token before it expires. Reactive refresh-on-401
 * cannot work here: the API's refresh endpoint validates the *access*
 * token's own lifetime, so once it expires the refresh token is unusable
 * (see `lib/refreshSchedule.ts` for the scheduling decisions).
 *
 * `generation` guards every async chain below against being superseded by a
 * later one — e.g. a logout landing while a `scheduleNext()` fetch, or a
 * `store.refresh()` call, is still in flight. `clearTimer()` bumps it (and
 * cancels whatever timer is currently pending); each chain captures its own
 * value up front and, after every await, checks it against the live one
 * before arming a timer or acting on a resolved fetch.
 */
export default defineNuxtPlugin(() => {
  const store = useAuthStore()
  let timer: ReturnType<typeof setTimeout> | null = null
  let generation = 0

  function clearTimer(): void {
    generation += 1
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }

  async function attemptRefresh(expSeconds: number, myGeneration: number): Promise<void> {
    const outcome = await store.refresh()

    if (isSuperseded(myGeneration, generation)) {
      return
    }

    switch (nextSchedulerAction(outcome, expSeconds)) {
      case 'reschedule':
        await scheduleNext()
        return
      case 'retry':
        timer = setTimeout(() => {
          timer = null
          void attemptRefresh(expSeconds, myGeneration)
        }, RETRY_DELAY_MS)
        return
      case 'stop':
        // 'unauthenticated' already cleared the user. A 'retry' that gave up
        // because the access token actually expired has not — do it here,
        // since the API can no longer refresh this pair by design.
        if (outcome === 'retry') {
          store.setUser(null)
        }
        return
    }
  }

  async function scheduleNext(): Promise<void> {
    clearTimer()
    const myGeneration = generation

    if (!store.isAuthenticated) {
      return
    }

    // /api/auth/me does not expose exp, so ask the server how long is left.
    const session = await $fetch<{ exp: number } | null>('/api/auth/session')

    if (isSuperseded(myGeneration, generation)) {
      return
    }

    if (!session) {
      store.setUser(null)
      return
    }

    timer = setTimeout(() => {
      timer = null
      void attemptRefresh(session.exp, myGeneration)
    }, msUntilRefresh(session.exp))
  }

  watch(() => store.isAuthenticated, () => { void scheduleNext() }, { immediate: true })
})
