import type { Ref } from 'vue'
import { onUnmounted, watch } from 'vue'

export interface UseInfiniteScrollOptions {
  root?: Element | Document | null
  rootMargin?: string
  threshold?: number | number[]
}

/**
 * Invokes `onIntersect` whenever `target` (a ref to a sentinel element,
 * typically a Vue template ref) becomes visible, using the native
 * `IntersectionObserver` API directly -- no `@vueuse/core`.
 *
 * Generic and reusable: this knows nothing about films, cursors, or "is
 * there a next page" -- callers express that by conditionally rendering
 * (`v-if`) the element `target` points at. `watch(target, ...)` reacts to
 * the ref becoming a real element (attach) or `null` (detach), so a
 * sentinel that appears/disappears as server state changes (e.g. `pageInfo`
 * after a search) gets connect/disconnect handled automatically, with no
 * extra wiring at the call site. The observer is also always torn down on
 * unmount as a backstop.
 *
 * Re-entrancy ("don't fire again while already loading") is handled here,
 * not by the caller: `onIntersect` is awaited, and further intersections
 * are ignored until it settles. This only depends on the callback's own
 * promise lifecycle, not on any externally-supplied "pending" flag, which
 * keeps the composable callback-only and fully generic.
 */
export function useInfiniteScroll(
  target: Ref<Element | null>,
  onIntersect: () => void | Promise<void>,
  options: UseInfiniteScrollOptions = {},
): void {
  let observer: IntersectionObserver | null = null
  let isHandling = false

  async function handleEntries(entries: IntersectionObserverEntry[]): Promise<void> {
    if (isHandling) return
    if (!entries.some(entry => entry.isIntersecting)) return

    isHandling = true
    try {
      await onIntersect()
    }
    finally {
      isHandling = false
    }
  }

  function teardown(): void {
    observer?.disconnect()
    observer = null
  }

  watch(target, (element) => {
    teardown()
    if (!element) return

    observer = new IntersectionObserver(entries => void handleEntries(entries), options)
    observer.observe(element)
  }, { immediate: true })

  onUnmounted(teardown)
}
