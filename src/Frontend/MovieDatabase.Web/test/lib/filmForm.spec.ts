import { describe, expect, it } from 'vitest'
import { hydrateFormState, sanitizeFilmFormValue, toEditFilmInput, toFilmFormValue } from '../../lib/filmForm'

const baseValue = {
  title: 'Arrival',
  releaseDate: '2016-11-11',
  description: 'Linguists meet heptapods.',
  director: { name: 'Denis', surname: 'Villeneuve' },
  producer: { name: 'Shawn Levy' },
  actors: [] as Array<{ name: string, surname: string }>,
  genres: [] as Array<{ name: string }>,
}

describe('sanitizeFilmFormValue', () => {
  it('drops actor rows with a blank name', () => {
    const result = sanitizeFilmFormValue({
      ...baseValue,
      actors: [
        { name: 'Amy', surname: 'Adams' },
        { name: '', surname: '' },
      ],
    })

    expect(result.actors).toEqual([{ name: 'Amy', surname: 'Adams' }])
  })

  it('drops actor rows whose name is whitespace only', () => {
    const result = sanitizeFilmFormValue({
      ...baseValue,
      actors: [{ name: '   ', surname: 'Renner' }],
    })

    expect(result.actors).toEqual([])
  })

  it('keeps an actor with a name but a blank surname', () => {
    const result = sanitizeFilmFormValue({
      ...baseValue,
      actors: [{ name: 'Amy', surname: '' }],
    })

    expect(result.actors).toEqual([{ name: 'Amy', surname: '' }])
  })

  it('drops genre rows with a blank name', () => {
    const result = sanitizeFilmFormValue({
      ...baseValue,
      genres: [{ name: 'Drama' }, { name: '' }, { name: '  ' }],
    })

    expect(result.genres).toEqual([{ name: 'Drama' }])
  })

  it('leaves title, releaseDate, description, director and producer untouched', () => {
    const result = sanitizeFilmFormValue(baseValue)

    expect(result.title).toBe(baseValue.title)
    expect(result.releaseDate).toBe(baseValue.releaseDate)
    expect(result.description).toBe(baseValue.description)
    expect(result.director).toEqual(baseValue.director)
    expect(result.producer).toEqual(baseValue.producer)
  })

  it('returns empty arrays when every actor and genre row is blank', () => {
    const result = sanitizeFilmFormValue({
      ...baseValue,
      actors: [{ name: '', surname: '' }],
      genres: [{ name: '' }],
    })

    expect(result.actors).toEqual([])
    expect(result.genres).toEqual([])
  })

  it('does not mutate the input value', () => {
    const input = {
      ...baseValue,
      actors: [{ name: '', surname: '' }],
      genres: [{ name: 'Drama' }],
    }

    sanitizeFilmFormValue(input)

    expect(input.actors).toEqual([{ name: '', surname: '' }])
  })
})

// A fetched film as `GetFilmByIdQuery` returns it -- ids present throughout,
// exactly as `toFilmFormValue` expects.
const fetchedFilm = {
  id: 'film-1',
  title: 'Arrival',
  description: 'Linguists meet heptapods.',
  releaseDate: '2016-11-11',
  thumbnailUrl: null,
  director: { id: 'director-1', name: 'Denis', surname: 'Villeneuve' },
  producer: { id: 'producer-1', name: 'Shawn Levy' },
  actors: [
    { id: 'actor-1', name: 'Amy', surname: 'Adams' },
    { id: 'actor-2', name: 'Jeremy', surname: 'Renner' },
  ],
  genres: [
    { id: 'genre-1', name: 'Drama' },
    { id: 'genre-2', name: 'Sci-Fi' },
  ],
}

describe('toFilmFormValue', () => {
  it('carries the director, producer, actor and genre ids from the fetched film', () => {
    const value = toFilmFormValue(fetchedFilm)

    expect(value.director.id).toBe('director-1')
    expect(value.producer.id).toBe('producer-1')
    expect(value.actors.map(actor => actor.id)).toEqual(['actor-1', 'actor-2'])
    expect(value.genres.map(genre => genre.id)).toEqual(['genre-1', 'genre-2'])
  })

  it('carries the names untouched alongside the ids', () => {
    const value = toFilmFormValue(fetchedFilm)

    expect(value.title).toBe('Arrival')
    expect(value.releaseDate).toBe('2016-11-11')
    expect(value.description).toBe('Linguists meet heptapods.')
    expect(value.director).toEqual({ id: 'director-1', name: 'Denis', surname: 'Villeneuve' })
    expect(value.producer).toEqual({ id: 'producer-1', name: 'Shawn Levy' })
    expect(value.actors).toEqual([
      { id: 'actor-1', name: 'Amy', surname: 'Adams' },
      { id: 'actor-2', name: 'Jeremy', surname: 'Renner' },
    ])
    expect(value.genres).toEqual([
      { id: 'genre-1', name: 'Drama' },
      { id: 'genre-2', name: 'Sci-Fi' },
    ])
  })

  it('defaults a null description to an empty string', () => {
    const value = toFilmFormValue({ ...fetchedFilm, description: null })

    expect(value.description).toBe('')
  })

  it('produces empty arrays for a film with no cast or genres', () => {
    const value = toFilmFormValue({ ...fetchedFilm, actors: [], genres: [] })

    expect(value.actors).toEqual([])
    expect(value.genres).toEqual([])
  })

  it('never populates thumbnailId from a fetched film (the raw blob id is never exposed over GraphQL)', () => {
    const value = toFilmFormValue(fetchedFilm)

    expect(value.thumbnailId).toBeUndefined()
  })
})

describe('toEditFilmInput', () => {
  it('carries the director, producer, actor and genre ids into the mutation input', () => {
    const value = toFilmFormValue(fetchedFilm)

    const input = toEditFilmInput('film-1', value)

    expect(input.id).toBe('film-1')
    expect(input.director).toEqual({ id: 'director-1', name: 'Denis', surname: 'Villeneuve' })
    expect(input.producer).toEqual({ id: 'producer-1', name: 'Shawn Levy' })
    expect(input.actors).toEqual([
      { id: 'actor-1', name: 'Amy', surname: 'Adams' },
      { id: 'actor-2', name: 'Jeremy', surname: 'Renner' },
    ])
    expect(input.genres).toEqual([
      { id: 'genre-1', name: 'Drama' },
      { id: 'genre-2', name: 'Sci-Fi' },
    ])
  })

  it('carries the given film id and the remaining fields untouched', () => {
    const input = toEditFilmInput('film-1', baseValue)

    expect(input).toEqual({
      id: 'film-1',
      title: baseValue.title,
      releaseDate: baseValue.releaseDate,
      description: baseValue.description,
      director: { id: undefined, name: baseValue.director.name, surname: baseValue.director.surname },
      producer: { id: undefined, name: baseValue.producer.name },
      actors: [],
      genres: [],
      thumbnail: null,
    })
  })

  it('round-trips a fetched film through the form value and back into the mutation input with the same ids', () => {
    const value = toFilmFormValue(fetchedFilm)
    const input = toEditFilmInput(fetchedFilm.id, value)

    expect(input.director?.id).toBe(fetchedFilm.director.id)
    expect(input.producer?.id).toBe(fetchedFilm.producer.id)
    expect(input.actors.map(actor => actor?.id)).toEqual(fetchedFilm.actors.map(actor => actor.id))
    expect(input.genres.map(genre => genre?.id)).toEqual(fetchedFilm.genres.map(genre => genre.id))
  })

  it('does not leak an id onto a new actor/genre row that never had one (create flow)', () => {
    const value = {
      ...baseValue,
      actors: [{ name: 'Amy', surname: 'Adams' }],
      genres: [{ name: 'Drama' }],
    }

    const input = toEditFilmInput('film-1', value)

    expect(input.actors[0]?.id).toBeUndefined()
    expect(input.genres[0]?.id).toBeUndefined()
  })

  it('carries thumbnailId through as thumbnail when a new image was uploaded this session', () => {
    const input = toEditFilmInput('film-1', { ...baseValue, thumbnailId: 'blob-123' })

    expect(input.thumbnail).toBe('blob-123')
  })

  it('sends thumbnail: null when no new image was uploaded this session, to preserve the existing one', () => {
    const input = toEditFilmInput('film-1', baseValue)

    expect(input.thumbnail).toBeNull()
  })
})

// A complete FilmFormValue with ids, as `toFilmFormValue` would hand to
// `FilmForm`'s `initial` prop for the edit page.
const hydratedInitial = {
  title: 'Arrival',
  releaseDate: '2016-11-11',
  description: 'Linguists meet heptapods.',
  director: { id: 'director-1', name: 'Denis', surname: 'Villeneuve' },
  producer: { id: 'producer-1', name: 'Shawn Levy' },
  actors: [{ id: 'actor-1', name: 'Amy', surname: 'Adams' }],
  genres: [{ id: 'genre-1', name: 'Drama' }],
}

describe('hydrateFormState', () => {
  it('carries the director, producer, actor and genre ids from initial into the hydrated state', () => {
    // Regression test for the bug caught in this task's own self-review:
    // this hydration previously rebuilt director/producer by destructuring
    // `{ name: initial.director.name, surname: initial.director.surname }`
    // (and the equivalent for producer), which silently dropped whatever
    // `id` `initial` carried even though actors/genres (spread whole) kept
    // theirs. This is the one assertion that would have failed against that
    // version.
    const state = hydrateFormState(hydratedInitial)

    expect(state.director.id).toBe('director-1')
    expect(state.producer.id).toBe('producer-1')
    expect(state.actors[0]?.id).toBe('actor-1')
    expect(state.genres[0]?.id).toBe('genre-1')
  })

  it('carries the names and other fields through untouched', () => {
    const state = hydrateFormState(hydratedInitial)

    expect(state).toEqual(hydratedInitial)
  })

  it('starts every field blank, with ids left absent rather than defaulted to a placeholder, when initial is null (create flow)', () => {
    const state = hydrateFormState(null)

    expect(state).toEqual({
      title: '',
      releaseDate: '',
      description: '',
      director: { id: undefined, name: '', surname: '' },
      producer: { id: undefined, name: '' },
      actors: [],
      genres: [],
    })
    expect(state.director.id).toBeUndefined()
    expect(state.producer.id).toBeUndefined()
  })

  it('returns fresh actor/genre rows rather than the same array/object references as initial', () => {
    const state = hydrateFormState(hydratedInitial)

    expect(state.actors).not.toBe(hydratedInitial.actors)
    expect(state.actors[0]).not.toBe(hydratedInitial.actors[0])
    expect(state.genres).not.toBe(hydratedInitial.genres)
    expect(state.genres[0]).not.toBe(hydratedInitial.genres[0])
  })
})
