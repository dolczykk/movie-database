export const DEFAULT_LEAD_SECONDS = 300

/** Fixed delay between retry attempts while a refresh outcome is indeterminate. */
export const RETRY_DELAY_MS = 30_000

export function msUntilRefresh(
  expSeconds: number,
  now = Date.now(),
  leadSeconds = DEFAULT_LEAD_SECONDS,
): number {
  const refreshAt = (expSeconds - leadSeconds) * 1000
  return Math.max(0, refreshAt - now)
}

export type SchedulerAction = 'reschedule' | 'retry' | 'stop'

/**
 * Decides what the scheduler should do next after a `store.refresh()` attempt.
 *
 * - `success` -> `reschedule` against the new token's expiry.
 * - `unauthenticated` -> `stop`; the store has already cleared the user.
 * - `retry` -> `retry` after a short fixed delay, unless the access token has
 *   actually expired — the API can't refresh an expired access token by
 *   design, so there is nothing left to retry and the scheduler must `stop`.
 */
export function nextSchedulerAction(
  outcome: 'success' | 'unauthenticated' | 'retry',
  expSeconds: number,
  now = Date.now(),
): SchedulerAction {
  if (outcome === 'success') {
    return 'reschedule'
  }

  if (outcome === 'unauthenticated') {
    return 'stop'
  }

  return expSeconds * 1000 <= now ? 'stop' : 'retry'
}

/**
 * Whether an async chain that captured `capturedGeneration` has been
 * superseded by a later one — e.g. a logout landing while a `scheduleNext()`
 * fetch, or a `store.refresh()` call, is still in flight. A chain that
 * finds itself superseded after an await must abandon silently: arm no
 * timer, touch no store state.
 */
export function isSuperseded(capturedGeneration: number, currentGeneration: number): boolean {
  return capturedGeneration !== currentGeneration
}
