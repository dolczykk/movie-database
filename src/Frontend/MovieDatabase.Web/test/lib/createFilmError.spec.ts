import { describe, expect, it } from 'vitest'
import { describeCreateFilmError } from '../../lib/createFilmError'

describe('describeCreateFilmError', () => {
  it('reports a user-correctable message for a duplicate title', () => {
    const error = new Error('Film with the same title exists')

    expect(describeCreateFilmError(error)).toBe('A film with that title already exists. Choose a different title.')
  })

  it('falls back to a generic message for any other Error', () => {
    const error = new Error('Unexpected server error')

    expect(describeCreateFilmError(error)).toBe('The film could not be created.')
  })

  it('falls back to a generic message when the thrown value is not an Error', () => {
    expect(describeCreateFilmError('some string')).toBe('The film could not be created.')
    expect(describeCreateFilmError(null)).toBe('The film could not be created.')
    expect(describeCreateFilmError(undefined)).toBe('The film could not be created.')
  })
})
