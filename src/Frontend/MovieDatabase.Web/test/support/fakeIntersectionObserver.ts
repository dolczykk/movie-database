/**
 * `happy-dom` has no `IntersectionObserver`, and `@nuxt/test-utils`'s own
 * environment mock is a no-op (`observe`/`disconnect` do nothing, the
 * callback is never invoked). This fake is real enough to drive
 * `useInfiniteScroll`'s tests: it tracks observed elements and lets a test
 * manually `trigger()` an intersection callback, as the browser would when
 * scrolling the sentinel into/out of view.
 */
export class FakeIntersectionObserver implements IntersectionObserver {
  static instances: FakeIntersectionObserver[] = []

  readonly root: Element | Document | null
  readonly rootMargin: string
  readonly thresholds: ReadonlyArray<number>
  readonly observedElements = new Set<Element>()

  constructor(
    private readonly callback: IntersectionObserverCallback,
    options?: IntersectionObserverInit,
  ) {
    this.root = options?.root ?? null
    this.rootMargin = options?.rootMargin ?? ''
    this.thresholds = ([] as number[]).concat(options?.threshold ?? 0)
    FakeIntersectionObserver.instances.push(this)
  }

  observe(element: Element): void {
    this.observedElements.add(element)
  }

  unobserve(element: Element): void {
    this.observedElements.delete(element)
  }

  disconnect(): void {
    this.observedElements.clear()
  }

  takeRecords(): IntersectionObserverEntry[] {
    return []
  }

  /** Simulates the browser reporting every currently-observed element's
   *  intersection state in one callback invocation. */
  trigger(isIntersecting: boolean): void {
    const entries = [...this.observedElements].map(target => ({ isIntersecting, target }))
    this.callback(entries as IntersectionObserverEntry[], this)
  }

  static reset(): void {
    FakeIntersectionObserver.instances = []
  }

  static latest(): FakeIntersectionObserver {
    const instance = this.instances.at(-1)
    if (!instance) throw new Error('No IntersectionObserver has been constructed yet')
    return instance
  }
}
