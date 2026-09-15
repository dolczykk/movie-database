import { describe, expect, it } from 'vitest'
import { isValidFilmId } from '../../lib/filmId'

describe('isValidFilmId', () => {
  it('accepts a canonical lowercase UUID', () => {
    expect(isValidFilmId('123e4567-e89b-12d3-a456-426614174000')).toBe(true)
  })

  it('accepts an uppercase UUID', () => {
    expect(isValidFilmId('123E4567-E89B-12D3-A456-426614174000')).toBe(true)
  })

  it('rejects an empty string', () => {
    expect(isValidFilmId('')).toBe(false)
  })

  it('rejects a non-UUID string', () => {
    expect(isValidFilmId('not-a-uuid')).toBe(false)
  })

  it('rejects a UUID missing hyphens', () => {
    expect(isValidFilmId('123e4567e89b12d3a456426614174000')).toBe(false)
  })

  it('rejects a UUID with the wrong segment lengths', () => {
    expect(isValidFilmId('123e456-e89b-12d3-a456-426614174000')).toBe(false)
  })

  it('rejects a UUID with a trailing path segment (route param smuggling)', () => {
    expect(isValidFilmId('123e4567-e89b-12d3-a456-426614174000/edit')).toBe(false)
  })

  it('rejects a numeric id', () => {
    expect(isValidFilmId('12345')).toBe(false)
  })
})
