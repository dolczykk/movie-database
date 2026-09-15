import { describe, expect, it } from 'vitest'
import { isSuperseded, msUntilRefresh, nextSchedulerAction } from '../../lib/refreshSchedule'

const exp = 1_000 // expires at t = 1_000_000 ms

describe('msUntilRefresh', () => {
  it('schedules the refresh one lead-window before expiry', () => {
    // now = 0, lead = 300s -> refresh at 700s
    expect(msUntilRefresh(exp, 0, 300)).toBe(700_000)
  })

  it('returns 0 when already inside the lead window', () => {
    expect(msUntilRefresh(exp, 800_000, 300)).toBe(0)
  })

  it('returns 0 when already expired', () => {
    expect(msUntilRefresh(exp, 2_000_000, 300)).toBe(0)
  })

  it('defaults to a five minute lead', () => {
    expect(msUntilRefresh(exp, 0)).toBe(700_000)
  })
})

describe('nextSchedulerAction', () => {
  it('reschedules on success, regardless of the expiry', () => {
    expect(nextSchedulerAction('success', exp, 0)).toBe('reschedule')
    expect(nextSchedulerAction('success', exp, 2_000_000)).toBe('reschedule')
  })

  it('stops on unauthenticated, regardless of the expiry', () => {
    expect(nextSchedulerAction('unauthenticated', exp, 0)).toBe('stop')
    expect(nextSchedulerAction('unauthenticated', exp, 2_000_000)).toBe('stop')
  })

  it('retries when the access token has not expired yet', () => {
    expect(nextSchedulerAction('retry', exp, 999_999)).toBe('retry')
  })

  it('stops once the access token has actually expired', () => {
    expect(nextSchedulerAction('retry', exp, 1_000_000)).toBe('stop')
  })

  it('stops once the access token is well past expiry', () => {
    expect(nextSchedulerAction('retry', exp, 2_000_000)).toBe('stop')
  })
})

describe('isSuperseded', () => {
  it('is false when the chain is still the current generation', () => {
    expect(isSuperseded(1, 1)).toBe(false)
  })

  it('is true once a later chain has bumped the generation', () => {
    expect(isSuperseded(1, 2)).toBe(true)
  })

  it('is true even if the generation has moved on by more than one step', () => {
    expect(isSuperseded(1, 5)).toBe(true)
  })
})
