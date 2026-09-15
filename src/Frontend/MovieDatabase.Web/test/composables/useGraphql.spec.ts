import type { TypedDocumentNode } from '@graphql-typed-document-node/core'
import { beforeEach, describe, expect, it, vi } from 'vitest'

// `useGraphql` is Nuxt-coupled: it reads `import.meta.server` and calls
// `useRequestHeaders` to decide whether to forward the incoming request's
// cookie header. Neither is controllable from this harness — Vitest here
// runs under plain `happy-dom` (not Nuxt's `environment: 'nuxt'`), and the
// bundler inlines `import.meta.server` to a static `false` for this build,
// confirmed empirically (a probe spec logging `import.meta.server` printed
// `false` unconditionally, with no way to flip it per test). So the
// cookie-forwarding decision is extracted into `runGraphql`, which takes the
// resolved `headers` as a plain argument and is fully testable; `useGraphql`
// is a three-line adapter around it that is exercised here too, but only
// exposes the client-side branch (headers `undefined`) given the above.
const fetchMock = vi.fn()
vi.stubGlobal('$fetch', fetchMock)

// `print` is wrapped (not replaced) so the memoisation tests below can count
// calls while every other test still gets real printed output. An ESM
// module namespace isn't configurable, so `vi.spyOn` on the live import
// doesn't work here -- this has to go through `vi.mock`.
vi.mock('graphql', async (importOriginal) => {
  const actual = await importOriginal<typeof import('graphql')>()
  return { ...actual, print: vi.fn(actual.print) }
})

const { parse, print } = await import('graphql')
const printMock = vi.mocked(print)

const { runGraphql, useGraphql } = await import('../../app/composables/useGraphql')

interface TestResult {
  film: { id: string, title: string }
}
interface TestVars extends Record<string, unknown> {
  id: string
}

const TEST_DOCUMENT = parse(
  'query TestQuery($id: ID!) { film(id: $id) { id title } }',
) as TypedDocumentNode<TestResult, TestVars>
const PRINTED_QUERY = print(TEST_DOCUMENT)
const VARIABLES: TestVars = { id: '1' }
const RESULT_DATA: TestResult = { film: { id: '1', title: 'Foo' } }

beforeEach(() => {
  fetchMock.mockReset()
  printMock.mockClear()
})

describe('runGraphql', () => {
  it('posts the printed document and the given variables to /api/graphql', async () => {
    fetchMock.mockResolvedValueOnce({ data: RESULT_DATA })

    await runGraphql(TEST_DOCUMENT, VARIABLES)

    expect(fetchMock).toHaveBeenCalledWith('/api/graphql', {
      method: 'POST',
      headers: undefined,
      body: { query: PRINTED_QUERY, variables: VARIABLES },
    })
  })

  it('returns the data from a successful response', async () => {
    fetchMock.mockResolvedValueOnce({ data: RESULT_DATA })

    const result = await runGraphql(TEST_DOCUMENT, VARIABLES)

    expect(result).toEqual(RESULT_DATA)
  })

  it('forwards the given headers untouched (the server-side cookie-forwarding case)', async () => {
    fetchMock.mockResolvedValueOnce({ data: RESULT_DATA })

    await runGraphql(TEST_DOCUMENT, VARIABLES, { cookie: 'access_token=abc' })

    expect(fetchMock).toHaveBeenCalledWith('/api/graphql', expect.objectContaining({
      headers: { cookie: 'access_token=abc' },
    }))
  })

  it('sends no headers when none are given (the client-side case)', async () => {
    fetchMock.mockResolvedValueOnce({ data: RESULT_DATA })

    await runGraphql(TEST_DOCUMENT, VARIABLES)

    const [, options] = fetchMock.mock.calls[0] as [string, { headers?: unknown }]
    expect(options.headers).toBeUndefined()
  })

  it('throws instead of returning undefined data when the response carries a GraphQL errors array', async () => {
    fetchMock.mockResolvedValueOnce({ errors: [{ message: 'Film not found' }] })

    await expect(runGraphql(TEST_DOCUMENT, VARIABLES)).rejects.toMatchObject({
      statusCode: 400,
      statusMessage: 'Film not found',
    })
  })

  it('memoises print(document) per document instead of re-printing on every call', async () => {
    // A document of its own (not TEST_DOCUMENT), so an earlier test's call
    // hasn't already warmed the module-level memoisation cache for it.
    const document = parse('query Memo($id: ID!) { film(id: $id) { id } }') as TypedDocumentNode<TestResult, TestVars>
    fetchMock.mockResolvedValue({ data: RESULT_DATA })

    await runGraphql(document, VARIABLES)
    await runGraphql(document, { id: '2' })
    await runGraphql(document, { id: '3' })

    expect(printMock).toHaveBeenCalledTimes(1)
  })

  it('prints a different document separately (memoisation is keyed on document identity)', async () => {
    const documentA = parse('query MemoA { ping }') as TypedDocumentNode<{ ping: string }, Record<string, unknown>>
    const documentB = parse('query MemoB { pong }') as TypedDocumentNode<{ pong: string }, Record<string, unknown>>
    fetchMock.mockResolvedValue({ data: RESULT_DATA })

    await runGraphql(documentA, {})
    await runGraphql(documentB, {})

    expect(printMock).toHaveBeenCalledTimes(2)
  })
})

describe('useGraphql', () => {
  it('delegates to runGraphql, posting to /api/graphql with no headers (this harness always resolves client-side)', async () => {
    fetchMock.mockResolvedValueOnce({ data: RESULT_DATA })

    const result = await useGraphql(TEST_DOCUMENT, VARIABLES)

    expect(result).toEqual(RESULT_DATA)
    expect(fetchMock).toHaveBeenCalledWith('/api/graphql', {
      method: 'POST',
      headers: undefined,
      body: { query: PRINTED_QUERY, variables: VARIABLES },
    })
  })

  it('propagates a thrown error from a GraphQL errors array', async () => {
    fetchMock.mockResolvedValueOnce({ errors: [{ message: 'Film not found' }] })

    await expect(useGraphql(TEST_DOCUMENT, VARIABLES)).rejects.toMatchObject({
      statusCode: 400,
      statusMessage: 'Film not found',
    })
  })
})
