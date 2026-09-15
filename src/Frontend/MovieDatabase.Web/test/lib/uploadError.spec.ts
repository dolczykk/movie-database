import { describe, expect, it } from 'vitest'
import { describeUploadClientError, describeUploadFailure } from '../../lib/uploadError'

describe('describeUploadFailure', () => {
  it('names a preflight/CSRF problem when the upstream error mentions it', () => {
    const error = Object.assign(
      new Error('[POST] "https://internal-movie-api.internal:7204/graphql": 400 Bad Request'),
      { statusCode: 400, data: { errors: [{ message: 'Multi-part requests must include a GraphQL preflight header.' }] } },
    )

    expect(describeUploadFailure(error)).toMatch(/preflight/i)
  })

  it('matches a CSRF-worded message too, not only "preflight"', () => {
    const error = Object.assign(new Error('rejected'), {
      data: { errors: [{ message: 'CSRF protection: header missing.' }] },
    })

    expect(describeUploadFailure(error)).toMatch(/preflight|csrf/i)
  })

  it('falls back to a generic message for a network-style failure with no body', () => {
    expect(describeUploadFailure(new TypeError('fetch failed'))).toBe('Unable to reach the GraphQL API to upload the image.')
  })

  it('falls back to a generic message when the upstream body has no recognizable error', () => {
    const error = Object.assign(new Error('boom'), { data: { secret: 'upstream response body' } })

    expect(describeUploadFailure(error)).toBe('Unable to reach the GraphQL API to upload the image.')
  })

  it('does not leak the upstream endpoint or response body in the generic case', () => {
    const error = Object.assign(
      new Error('[POST] "https://internal-movie-api.internal:7204/graphql": 500 Internal Server Error'),
      { statusCode: 500, data: { secret: 'upstream response body' } },
    )

    const message = describeUploadFailure(error)

    expect(message).not.toContain('internal-movie-api')
    expect(message).not.toContain('upstream response body')
  })

  it('handles non-object errors without throwing', () => {
    expect(describeUploadFailure('boom')).toBe('Unable to reach the GraphQL API to upload the image.')
    expect(describeUploadFailure(null)).toBe('Unable to reach the GraphQL API to upload the image.')
    expect(describeUploadFailure(undefined)).toBe('Unable to reach the GraphQL API to upload the image.')
  })
})

describe('describeUploadClientError', () => {
  it('surfaces the statusMessage Nitro attached to the error body', () => {
    const error = Object.assign(new Error('[POST] "/api/upload": 401 Unauthorized'), {
      statusCode: 401,
      data: { statusCode: 401, statusMessage: 'Not authenticated' },
    })

    expect(describeUploadClientError(error)).toBe('Not authenticated')
  })

  it('surfaces a preflight-diagnostic statusMessage verbatim', () => {
    const error = Object.assign(new Error('[POST] "/api/upload": 502 Bad Gateway'), {
      statusCode: 502,
      data: { statusMessage: 'Upload rejected: the API requires a GraphQL preflight header this server did not send correctly (see server/utils/uploadClient.ts). This is a configuration problem, not something the visitor can fix.' },
    })

    expect(describeUploadClientError(error)).toMatch(/preflight/i)
  })

  it('falls back to a generic message when there is no statusMessage to read', () => {
    expect(describeUploadClientError(new TypeError('Failed to fetch'))).toBe('The image could not be uploaded.')
  })

  it('falls back to a generic message for a non-string statusMessage', () => {
    const error = Object.assign(new Error('boom'), { data: { statusMessage: 42 } })

    expect(describeUploadClientError(error)).toBe('The image could not be uploaded.')
  })

  it('handles non-object errors without throwing', () => {
    expect(describeUploadClientError('boom')).toBe('The image could not be uploaded.')
    expect(describeUploadClientError(null)).toBe('The image could not be uploaded.')
  })
})
