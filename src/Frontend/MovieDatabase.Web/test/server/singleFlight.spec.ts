import { describe, expect, it, vi } from 'vitest'
import { singleFlight } from '../../server/utils/singleFlight'

describe('singleFlight', () => {
  it('runs the function once for concurrent calls with the same key', async () => {
    const fn = vi.fn(async () => {
      await new Promise(resolve => setTimeout(resolve, 10))
      return 'result'
    })

    const [first, second] = await Promise.all([
      singleFlight('key', fn),
      singleFlight('key', fn),
    ])

    expect(fn).toHaveBeenCalledTimes(1)
    expect(first).toBe('result')
    expect(second).toBe('result')
  })

  it('runs the function separately for different keys', async () => {
    const fn = vi.fn(async () => 'result')

    await Promise.all([singleFlight('a', fn), singleFlight('b', fn)])

    expect(fn).toHaveBeenCalledTimes(2)
  })

  it('allows a new call after the previous one settles', async () => {
    const fn = vi.fn(async () => 'result')

    await singleFlight('key', fn)
    await singleFlight('key', fn)

    expect(fn).toHaveBeenCalledTimes(2)
  })

  it('does not cache rejections', async () => {
    const failing = vi.fn(async () => {
      throw new Error('boom')
    })

    await expect(singleFlight('key', failing)).rejects.toThrow('boom')
    await expect(singleFlight('key', failing)).rejects.toThrow('boom')

    expect(failing).toHaveBeenCalledTimes(2)
  })
})
