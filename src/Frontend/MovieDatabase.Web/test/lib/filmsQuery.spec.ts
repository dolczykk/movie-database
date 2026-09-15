import { describe, expect, it } from 'vitest'
import { accumulateFilmNodes, buildFilmsWhere, FILMS_PAGE_SIZE, nextFilmsCursor } from '../../lib/filmsQuery'

describe('FILMS_PAGE_SIZE', () => {
  it('never exceeds the API MaxPageSize of 20', () => {
    expect(FILMS_PAGE_SIZE).toBeLessThanOrEqual(20)
  })
})

describe('buildFilmsWhere', () => {
  it('returns null for an empty search string', () => {
    expect(buildFilmsWhere('')).toBeNull()
  })

  it('returns null for a whitespace-only search string', () => {
    expect(buildFilmsWhere('   ')).toBeNull()
  })

  it('builds a title-contains filter for a search term', () => {
    expect(buildFilmsWhere('arrival')).toEqual({ title: { contains: 'arrival' } })
  })

  it('trims surrounding whitespace before building the filter', () => {
    expect(buildFilmsWhere('  arrival  ')).toEqual({ title: { contains: 'arrival' } })
  })
})

describe('nextFilmsCursor', () => {
  it('returns the end cursor when present', () => {
    expect(nextFilmsCursor('cursor-123')).toBe('cursor-123')
  })

  it('falls back to null when the end cursor is null', () => {
    expect(nextFilmsCursor(null)).toBeNull()
  })

  it('falls back to null when the end cursor is undefined', () => {
    expect(nextFilmsCursor(undefined)).toBeNull()
  })
})

describe('accumulateFilmNodes', () => {
  it('starts an empty accumulation from the first page', () => {
    expect(accumulateFilmNodes([], ['a', 'b'], null)).toEqual(['a', 'b'])
  })

  it('replaces (does not append to) whatever was accumulated when the fetch was for the first page', () => {
    // A new search term (or clearing one) resets the cursor to null before
    // refetching -- the first-page result must win outright, not merge with
    // films left over from the previous search.
    expect(accumulateFilmNodes(['stale-a', 'stale-b'], ['fresh-a'], null)).toEqual(['fresh-a'])
  })

  it('appends to the existing accumulation when the fetch was for a later page ("Load more")', () => {
    expect(accumulateFilmNodes(['a', 'b'], ['c', 'd'], 'cursor-123')).toEqual(['a', 'b', 'c', 'd'])
  })

  it('produces just the incoming page when appending to an empty accumulation', () => {
    expect(accumulateFilmNodes([], ['a'], 'cursor-123')).toEqual(['a'])
  })

  it('resets to an empty page result rather than keeping stale films', () => {
    // A search with no matches must clear the grid, not leave the previous
    // search's results on screen.
    expect(accumulateFilmNodes(['stale'], [], null)).toEqual([])
  })
})
