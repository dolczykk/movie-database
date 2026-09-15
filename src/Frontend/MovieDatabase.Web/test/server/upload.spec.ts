import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// NOTE ON COVERAGE: like `test/server/graphql.spec.ts`, this mocks the
// project-owned seams (`readAuthCookies`, `postUploadMultipart`,
// `describeUploadFailure`) rather than exercising Nitro's real
// `readMultipartFormData`/`useRuntimeConfig`/`$fetch` auto-imports, which
// this project's plain-`happy-dom` Vitest harness cannot resolve. What this
// file covers is this route's own logic: gating on the access-token cookie
// before touching the request body or the API, requiring a `file` part,
// passing a successful envelope's `uploadBlob` through, turning a GraphQL
// error envelope into a 400, and turning an upstream failure into a 502
// whose message comes from `describeUploadFailure` (unit-tested separately
// in `test/lib/uploadError.spec.ts`). The real multipart wire format and the
// `GraphQL-Preflight` header this depends on are unverified end to end --
// see the task report.

const readAuthCookies = vi.fn()
vi.mock('../../server/utils/authCookies', () => ({ readAuthCookies }))

const postUploadMultipart = vi.fn()
vi.mock('../../server/utils/uploadClient', () => ({ postUploadMultipart }))

const describeUploadFailure = vi.fn()
vi.mock('../../lib/uploadError', () => ({ describeUploadFailure }))

vi.stubGlobal('defineEventHandler', (handler: unknown) => handler)

const { default: uploadHandler } = await import('../../server/api/upload.post')

const handler = uploadHandler as unknown as (event: unknown) => Promise<unknown>
const event = {} as never

const readMultipartFormData = vi.fn()
const getRequestHeader = vi.fn()
let consoleErrorSpy: ReturnType<typeof vi.spyOn>

const filePart = { name: 'file', data: Buffer.from('fake-image-bytes'), type: 'image/png', filename: 'poster.png' }

beforeEach(() => {
  readAuthCookies.mockReset()
  postUploadMultipart.mockReset()
  describeUploadFailure.mockReset()
  readMultipartFormData.mockReset()
  getRequestHeader.mockReset()

  vi.stubGlobal('readMultipartFormData', readMultipartFormData)
  vi.stubGlobal('getRequestHeader', getRequestHeader)

  readAuthCookies.mockReturnValue({ accessToken: 'access-token-value', refreshToken: 'refresh-token-value' })
  readMultipartFormData.mockResolvedValue([filePart])
  getRequestHeader.mockReturnValue(undefined)
  describeUploadFailure.mockReturnValue('mocked upload failure message')
  consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  vi.unstubAllGlobals()
  consoleErrorSpy.mockRestore()
})

describe('POST /api/upload', () => {
  it('rejects an unauthenticated request before reading the body or reaching the API', async () => {
    readAuthCookies.mockReturnValue({ accessToken: null, refreshToken: null })

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 401, statusMessage: 'Not authenticated' })

    expect(readMultipartFormData).not.toHaveBeenCalled()
    expect(postUploadMultipart).not.toHaveBeenCalled()
  })

  it('rejects an oversized request by content-length before reading the body', async () => {
    getRequestHeader.mockReturnValue('5000001')

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 413, statusMessage: 'Choose an image smaller than 5 MB.' })

    expect(readMultipartFormData).not.toHaveBeenCalled()
    expect(postUploadMultipart).not.toHaveBeenCalled()
  })

  it('allows a request at exactly the size limit through the content-length check', async () => {
    getRequestHeader.mockReturnValue('5000000')
    postUploadMultipart.mockResolvedValue({ data: { uploadBlob: { id: '1', fileName: 'a', url: 'a' } } })

    await handler(event)

    expect(readMultipartFormData).toHaveBeenCalled()
  })

  it('ignores a missing or non-numeric content-length header (falls through to post-read validation)', async () => {
    getRequestHeader.mockReturnValue(undefined)
    postUploadMultipart.mockResolvedValue({ data: { uploadBlob: { id: '1', fileName: 'a', url: 'a' } } })

    await handler(event)

    expect(readMultipartFormData).toHaveBeenCalled()
  })

  it('rejects when there is no file part', async () => {
    readMultipartFormData.mockResolvedValue([{ name: 'notes', data: Buffer.from('hi') }])

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 400, statusMessage: 'No file provided' })

    expect(postUploadMultipart).not.toHaveBeenCalled()
  })

  it('rejects when the form has no parts at all', async () => {
    readMultipartFormData.mockResolvedValue(undefined)

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 400, statusMessage: 'No file provided' })
  })

  it('rejects a disallowed content type on the actual file part, even when content-length passed', async () => {
    readMultipartFormData.mockResolvedValue([{ ...filePart, type: 'image/gif' }])

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 400, statusMessage: 'Choose a PNG, JPEG or WebP image.' })

    expect(postUploadMultipart).not.toHaveBeenCalled()
  })

  it('rejects a file part whose actual buffered size exceeds the limit, even when content-length undercounted it', async () => {
    getRequestHeader.mockReturnValue('10')
    readMultipartFormData.mockResolvedValue([{ ...filePart, data: Buffer.alloc(5_000_000) }])

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 400, statusMessage: 'Choose an image smaller than 5 MB.' })

    expect(postUploadMultipart).not.toHaveBeenCalled()
  })

  it('uploads the file part and returns the blob from a successful envelope', async () => {
    const blob = { id: 'blob-1', fileName: 'poster.jpg', url: 'https://blobs.local/content/poster.jpg' }
    postUploadMultipart.mockResolvedValue({ data: { uploadBlob: blob } })

    await expect(handler(event)).resolves.toEqual(blob)

    expect(postUploadMultipart).toHaveBeenCalledTimes(1)
    expect(postUploadMultipart).toHaveBeenCalledWith(event, filePart, 'access-token-value')
  })

  it('returns a 400 with the GraphQL error message from a successful-but-erroring envelope', async () => {
    postUploadMultipart.mockResolvedValue({ errors: [{ message: 'Choose a PNG, JPEG or WebP image.' }] })

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 400, statusMessage: 'Choose a PNG, JPEG or WebP image.' })
  })

  it('returns a 502 with an empty-response message when the envelope carries neither data nor errors', async () => {
    postUploadMultipart.mockResolvedValue({})

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 502, statusMessage: 'Empty upload response' })
  })

  it('returns a 502 whose message comes from describeUploadFailure when the upstream request throws', async () => {
    const upstreamError = new TypeError('fetch failed')
    postUploadMultipart.mockRejectedValue(upstreamError)

    await expect(handler(event)).rejects.toMatchObject({ statusCode: 502, statusMessage: 'mocked upload failure message' })

    expect(describeUploadFailure).toHaveBeenCalledWith(upstreamError)
    expect(consoleErrorSpy).toHaveBeenCalledWith('[api/upload] upstream request failed', upstreamError.message)
  })
})
