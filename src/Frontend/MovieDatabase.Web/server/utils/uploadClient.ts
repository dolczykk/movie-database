import type { H3Event } from 'h3'

export interface UploadedBlob {
  id: string
  fileName: string
  url: string
}

export interface UploadBlobEnvelope {
  data?: { uploadBlob: UploadedBlob }
  errors?: Array<{ message: string }>
}

const UPLOAD_BLOB_MUTATION = `
  mutation UploadBlob($file: Upload!) {
    uploadBlob(file: $file) {
      id
      fileName
      url
    }
  }
`

/**
 * Posts one file to the GraphQL API using the GraphQL multipart request
 * specification (`operations` + `map` + the file itself), because the
 * `Upload` scalar (registered via `AddType<UploadType>()` in `Program.cs`)
 * cannot be sent as plain JSON the way `postGraphqlRaw`/`callGraphql`
 * (`graphqlClient.ts`) send every other operation. Unlike those, this
 * always sends exactly one hardcoded operation (`uploadBlob`), so it builds
 * the multipart body itself instead of taking one from the caller.
 *
 * Sends a `GraphQL-Preflight: 1` header on every request -- HotChocolate's
 * CSRF protection for multipart requests. The API configures GraphQL with a
 * bare `app.MapGraphQL()` (no custom `GraphQLServerOptions`), which is
 * documented as the default the API's pinned `HotChocolate.AspNetCore`
 * 15.1.12 package expects for that header. That package's compiled strings
 * do contain the literal header name `"GraphQL-Preflight"` paired with an
 * expected value of `"1"`, confirming this is the right header/value for
 * this exact version -- but that is static evidence read out of the
 * installed package, not a live request against a running API: this has
 * not been exercised end to end (Docker/Aspire is not available in this
 * environment). If the header or value is wrong, HotChocolate rejects the
 * request before executing it and the response's `errors[0].message`
 * mentions "preflight" -- `describeUploadFailure` (`lib/uploadError.ts`)
 * is what turns that into a legible message for `upload.post.ts` to report.
 */
export async function postUploadMultipart(
  event: H3Event,
  filePart: { data: Buffer, type?: string, filename?: string },
  accessToken: string,
): Promise<UploadBlobEnvelope> {
  const form = new FormData()
  form.append('operations', JSON.stringify({
    query: UPLOAD_BLOB_MUTATION,
    variables: { file: null },
  }))
  form.append('map', JSON.stringify({ 0: ['variables.file'] }))
  form.append(
    '0',
    // `filePart.data` is a Node `Buffer`, whose `.buffer` is typed as
    // `ArrayBufferLike` (it may be backed by a `SharedArrayBuffer`) --
    // not assignable to `BlobPart`, which requires a real `ArrayBuffer`.
    // Copying into a fresh `Uint8Array` gives it its own plain
    // `ArrayBuffer` and satisfies the type.
    new Blob([new Uint8Array(filePart.data)], { type: filePart.type ?? 'application/octet-stream' }),
    filePart.filename ?? 'upload',
  )

  const endpoint = useRuntimeConfig(event).graphqlEndpoint

  return await $fetch<UploadBlobEnvelope>(endpoint, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${accessToken}`,
      'GraphQL-Preflight': '1',
    },
    body: form,
  })
}
