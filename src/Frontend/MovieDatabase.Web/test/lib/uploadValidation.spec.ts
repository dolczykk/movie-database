import { describe, expect, it } from 'vitest'
import { MAX_UPLOAD_BYTES, validateUpload } from '../../lib/uploadValidation'

describe('validateUpload', () => {
  it('accepts every content type the API allows', () => {
    for (const type of ['image/png', 'image/jpeg', 'image/jpg', 'image/webp']) {
      expect(validateUpload({ type, size: 1_000 })).toBeNull()
    }
  })

  it('rejects a disallowed content type', () => {
    expect(validateUpload({ type: 'image/gif', size: 1_000 })).toMatch(/PNG|JPEG|WebP/i)
  })

  it('rejects a file at or above the size limit', () => {
    expect(validateUpload({ type: 'image/png', size: MAX_UPLOAD_BYTES })).toMatch(/5 MB/)
  })

  it('accepts a file just under the limit', () => {
    expect(validateUpload({ type: 'image/png', size: MAX_UPLOAD_BYTES - 1 })).toBeNull()
  })
})
