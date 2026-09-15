import { describe, expect, it } from 'vitest'
import { selectFilm } from '../../lib/filmDetail'

describe('selectFilm', () => {
  it('returns the first node when one is present', () => {
    const film = { id: '1', title: 'Arrival' }
    expect(selectFilm({ films: { nodes: [film] } })).toBe(film)
  })

  it('ignores any nodes beyond the first', () => {
    const first = { id: '1', title: 'Arrival' }
    const second = { id: '2', title: 'Contact' }
    expect(selectFilm({ films: { nodes: [first, second] } })).toBe(first)
  })

  it('returns null for an empty nodes array (id not found)', () => {
    expect(selectFilm({ films: { nodes: [] } })).toBeNull()
  })

  it('returns null when nodes is null', () => {
    expect(selectFilm({ films: { nodes: null } })).toBeNull()
  })

  it('returns null when films is null', () => {
    expect(selectFilm({ films: null })).toBeNull()
  })

  it('returns null when the query result is null', () => {
    expect(selectFilm(null)).toBeNull()
  })

  it('returns null when the query result is undefined', () => {
    expect(selectFilm(undefined)).toBeNull()
  })
})
