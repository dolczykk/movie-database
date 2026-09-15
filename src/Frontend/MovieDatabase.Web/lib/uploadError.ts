/**
 * Turns a failed upstream multipart request (`server/utils/uploadClient.ts`
 * calling the GraphQL API) into a message that names *why* it failed, when
 * that reason is diagnosable, instead of a blanket "upload failed".
 *
 * `uploadClient.ts` sends a `GraphQL-Preflight: 1` header on every request
 * -- HotChocolate's CSRF protection for the GraphQL multipart request spec.
 * That header name and value are an assumption, not something verified
 * against a running API: this repo cannot start the Aspire/Docker stack in
 * this environment, so no real request has been made (see the task
 * report). If the assumption is wrong, HotChocolate rejects the request
 * before executing it and reports a message containing the word
 * "preflight" (confirmed by inspecting the compiled strings in the
 * `HotChocolate.AspNetCore` 15.1.12 package this repo pins -- itself still
 * static evidence, not a live response). ofetch parses a JSON error body
 * onto the thrown error's `data`, so this looks there first and surfaces a
 * specific, actionable message for that one case; every other failure
 * (network error, unrelated 5xx, ...) gets a generic message that does not
 * repeat the upstream endpoint or response body back to the caller.
 */
export function describeUploadFailure(error: unknown): string {
  const message = extractGraphqlErrorMessage(error)

  if (message && /preflight|csrf/i.test(message)) {
    return 'Upload rejected: the API requires a GraphQL preflight header this server did not send correctly (see server/utils/uploadClient.ts). This is a configuration problem, not something the visitor can fix.'
  }

  return 'Unable to reach the GraphQL API to upload the image.'
}

function extractGraphqlErrorMessage(error: unknown): string | null {
  if (typeof error !== 'object' || error === null) {
    return null
  }

  const data = (error as { data?: unknown }).data
  if (data && typeof data === 'object') {
    const errors = (data as { errors?: Array<{ message?: unknown }> }).errors
    const nested = errors?.[0]?.message
    if (typeof nested === 'string') {
      return nested
    }
  }

  return null
}

/**
 * Turns a failed `POST /api/upload`, as `ImageUploader` sees it in the
 * browser, into a message it can show.
 *
 * `server/api/upload.post.ts` throws an h3 error with a `statusMessage`
 * crafted to be legible (the 401/400 cases in the route itself, and
 * `describeUploadFailure` above for an upstream failure). Nitro serializes
 * that onto the JSON error body, and ofetch parses it onto the thrown
 * error's `data` on the client -- the same shape `isUnauthorized`
 * (`app/stores/auth.ts`) and `isDefinitiveRejection`
 * (`server/api/auth/refresh.post.ts`) already rely on for `statusCode`
 * elsewhere in this codebase. Reading it here means the component surfaces
 * the crafted message (the preflight case included) instead of a hardcoded
 * generic string, without the component itself needing to know that shape.
 */
export function describeUploadClientError(error: unknown): string {
  if (typeof error === 'object' && error !== null) {
    const data = (error as { data?: unknown }).data
    if (data && typeof data === 'object') {
      const statusMessage = (data as { statusMessage?: unknown }).statusMessage
      if (typeof statusMessage === 'string' && statusMessage.length > 0) {
        return statusMessage
      }
    }
  }

  return 'The image could not be uploaded.'
}
