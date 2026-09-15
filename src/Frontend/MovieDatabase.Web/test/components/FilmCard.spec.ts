import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import FilmCard from '../../app/components/FilmCard.vue'

const film = {
  id: 'film-1',
  title: 'Arrival',
  description: 'Linguists meet heptapods.',
  releaseDate: '2016-11-11',
  thumbnailUrl: null,
  director: { id: 'd1', name: 'Denis', surname: 'Villeneuve' },
  genres: [{ id: 'g1', name: 'Science Fiction' }],
}

describe('FilmCard', () => {
  it('shows the title, director and genres', () => {
    render(FilmCard, { props: { film } })

    expect(screen.getByText('Arrival')).toBeTruthy()
    expect(screen.getByText(/Denis Villeneuve/)).toBeTruthy()
    expect(screen.getByText('Science Fiction')).toBeTruthy()
  })

  it('falls back to a placeholder when there is no thumbnail', () => {
    render(FilmCard, { props: { film } })

    expect(screen.queryByRole('img')).toBeNull()
  })

  it('renders the image when a thumbnail url is present', () => {
    render(FilmCard, { props: { film: { ...film, thumbnailUrl: 'https://cdn.test/content/film-1.jpg' } } })

    expect(screen.getByRole('img').getAttribute('src')).toBe('https://cdn.test/content/film-1.jpg')
  })
})
