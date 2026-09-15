/** Mirrors `Constants.Blob.MaxBlobSizeInBytes` -- the value the API uses as its own upload size limit. */
export const MAX_UPLOAD_BYTES = 5_000_000

/** Mirrors `Constants.Blob.AllowedContentTypes`. `image/jpg` is not a real IANA type, but the API allows it. */
export const ALLOWED_CONTENT_TYPES = ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']

/**
 * Client-side mirror of the checks the API makes -- a convenience so the
 * visitor fails fast on an obviously bad file, not a substitute for
 * server-side enforcement.
 *
 * The content type check mirrors the API's own: `UploadBlobRequestHandler`
 * independently rejects anything outside `Constants.Blob.AllowedContentTypes`,
 * so this check being wrong or bypassed cannot let a disallowed content
 * type through.
 *
 * The size check uses `>=` against `MAX_UPLOAD_BYTES`, the conservative
 * direction at the boundary. Whether the API actually rejects a file of
 * exactly that size is unverified: `UploadBlobRequestHandler` itself has no
 * explicit size check, and it is unconfirmed whether ASP.NET Core's
 * `MultipartBodyLengthLimit` (set to the same constant, in
 * `InfrastructureExtensions.cs`) rejects at that exact size or only above
 * it. This check is independent of upstream enforcement, not evidence for
 * it.
 */
export function validateUpload(file: { type: string, size: number }): string | null {
  if (!ALLOWED_CONTENT_TYPES.includes(file.type)) {
    return 'Choose a PNG, JPEG or WebP image.'
  }

  if (file.size >= MAX_UPLOAD_BYTES) {
    return 'Choose an image smaller than 5 MB.'
  }

  return null
}
