import { createError } from 'h3'
import { describeUploadFailure } from '../../lib/uploadError'
import { MAX_UPLOAD_BYTES, validateUpload } from '../../lib/uploadValidation'
import { readAuthCookies } from '../utils/authCookies'
import { postUploadMultipart } from '../utils/uploadClient'

/**
 * Multipart proxy for `uploadBlob`. `server/api/graphql.post.ts` forwards
 * JSON only and cannot carry the GraphQL multipart request spec the
 * `Upload` scalar needs, so this is its own route -- see
 * `server/utils/uploadClient.ts` for the multipart construction and the
 * `GraphQL-Preflight` header this depends on (unverified end to end, see
 * the task report).
 *
 * `uploadBlob` requires any authenticated user (`[Authorize]` with no
 * role), so this checks the access-token cookie itself, before reading the
 * request body or touching the API, rather than letting an unauthenticated
 * request reach HotChocolate only to be rejected there.
 */
export default defineEventHandler(async (event) => {
  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    throw createError({ statusCode: 401, statusMessage: 'Not authenticated' })
  }

  // Checked before `readMultipartFormData`, which buffers the *entire*
  // request body into Node memory with no limit of its own -- Nitro sets no
  // default max request size, and `MultipartBodyLengthLimit` downstream on
  // the API never gets a chance to reject anything if this process stalls
  // or OOMs first. Any authenticated user can POST this route directly, so
  // this is enforcement, not a convenience -- `lib/uploadValidation`'s
  // per-file check below only runs after the body is already buffered and
  // cannot protect against this on its own.
  const contentLength = Number(getRequestHeader(event, 'content-length'))
  if (Number.isFinite(contentLength) && contentLength > MAX_UPLOAD_BYTES) {
    throw createError({ statusCode: 413, statusMessage: 'Choose an image smaller than 5 MB.' })
  }

  const parts = await readMultipartFormData(event)
  const filePart = parts?.find(part => part.name === 'file')

  if (!filePart?.data) {
    throw createError({ statusCode: 400, statusMessage: 'No file provided' })
  }

  // Re-runs the same rules `lib/uploadValidation` gives the browser, against
  // the actual buffered part -- a missing `content-length` header (or one
  // that undercounts a chunked/multipart-inflated body) would otherwise let
  // an oversized or disallowed file straight through the check above.
  const validationError = validateUpload({ type: filePart.type ?? '', size: filePart.data.length })
  if (validationError) {
    throw createError({ statusCode: 400, statusMessage: validationError })
  }

  let response
  try {
    response = await postUploadMultipart(event, filePart, accessToken)
  }
  catch (error) {
    // Logs the message only -- see the matching comment in graphql.post.ts.
    // Here the raw error would also carry the multipart body and the
    // `Authorization` bearer header sent to the upstream API.
    console.error('[api/upload] upstream request failed', error instanceof Error ? error.message : String(error))
    throw createError({ statusCode: 502, statusMessage: describeUploadFailure(error) })
  }

  if (response.errors?.length) {
    throw createError({ statusCode: 400, statusMessage: response.errors[0]!.message })
  }

  if (!response.data) {
    throw createError({ statusCode: 502, statusMessage: 'Empty upload response' })
  }

  return response.data.uploadBlob
})
