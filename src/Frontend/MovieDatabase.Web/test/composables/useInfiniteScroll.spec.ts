import type { Ref } from 'vue'
import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, ref } from 'vue'
import { FakeIntersectionObserver } from '../support/fakeIntersectionObserver'

const { useInfiniteScroll } = await import('../../app/composables/useInfiniteScroll')

beforeEach(() => {
  FakeIntersectionObserver.reset()
  vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver)
})

function mountHost(target: Ref<Element | null>, onIntersect: () => void | Promise<void>) {
  return mount(defineComponent({
    setup() {
      useInfiniteScroll(target, onIntersect)
      return () => h('div')
    },
  }))
}

describe('useInfiniteScroll', () => {
  it('invokes the callback when the target reports intersecting', async () => {
    const target = ref<Element | null>(document.createElement('div'))
    const onIntersect = vi.fn()
    mountHost(target, onIntersect)
    await Promise.resolve()

    FakeIntersectionObserver.latest().trigger(true)

    expect(onIntersect).toHaveBeenCalledTimes(1)
  })

  it('does not invoke the callback when reported as not intersecting', async () => {
    const target = ref<Element | null>(document.createElement('div'))
    const onIntersect = vi.fn()
    mountHost(target, onIntersect)
    await Promise.resolve()

    FakeIntersectionObserver.latest().trigger(false)

    expect(onIntersect).not.toHaveBeenCalled()
  })

  it('does not invoke the callback again while a previous invocation is still in flight', async () => {
    let resolveFirst: () => void = () => {}
    const pending = new Promise<void>((resolve) => { resolveFirst = resolve })
    const onIntersect = vi.fn().mockReturnValueOnce(pending).mockReturnValue(undefined)
    const target = ref<Element | null>(document.createElement('div'))
    mountHost(target, onIntersect)
    await Promise.resolve()

    FakeIntersectionObserver.latest().trigger(true)
    FakeIntersectionObserver.latest().trigger(true)
    expect(onIntersect).toHaveBeenCalledTimes(1)

    resolveFirst()
    await pending
    await Promise.resolve()
    FakeIntersectionObserver.latest().trigger(true)

    expect(onIntersect).toHaveBeenCalledTimes(2)
  })

  it('does not construct an observer while the target is still null', () => {
    const target = ref<Element | null>(null)
    mountHost(target, vi.fn())

    expect(FakeIntersectionObserver.instances).toHaveLength(0)
  })

  it('disconnects the observer when the target becomes null again', async () => {
    const target = ref<Element | null>(document.createElement('div'))
    mountHost(target, vi.fn())
    await Promise.resolve()
    const observer = FakeIntersectionObserver.latest()
    const disconnectSpy = vi.spyOn(observer, 'disconnect')

    target.value = null
    await Promise.resolve()

    expect(disconnectSpy).toHaveBeenCalled()
  })

  it('disconnects the observer on unmount', async () => {
    const target = ref<Element | null>(document.createElement('div'))
    const wrapper = mountHost(target, vi.fn())
    await Promise.resolve()
    const observer = FakeIntersectionObserver.latest()
    const disconnectSpy = vi.spyOn(observer, 'disconnect')

    wrapper.unmount()

    expect(disconnectSpy).toHaveBeenCalled()
  })

  it('passes root/rootMargin/threshold through to the native constructor', async () => {
    const target = ref<Element | null>(document.createElement('div'))
    mount(defineComponent({
      setup() {
        useInfiniteScroll(target, vi.fn(), { rootMargin: '200px', threshold: 0.5 })
        return () => h('div')
      },
    }))
    await Promise.resolve()

    const observer = FakeIntersectionObserver.latest()
    expect(observer.rootMargin).toBe('200px')
    expect(observer.thresholds).toEqual([0.5])
  })

  it('does not throw when unmounting before any target was ever assigned', () => {
    const target = ref<Element | null>(null)
    const wrapper = mountHost(target, vi.fn())

    expect(() => wrapper.unmount()).not.toThrow()
  })
})
