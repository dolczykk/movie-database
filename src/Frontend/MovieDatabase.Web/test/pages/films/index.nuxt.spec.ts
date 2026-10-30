import { registerEndpoint, renderSuspended } from '@nuxt/test-utils/runtime'
import { fireEvent, screen, waitFor } from '@testing-library/vue'
import { readBody } from 'h3'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { FakeIntersectionObserver } from '../../support/fakeIntersectionObserver'
import FilmsIndexPage from '../../../app/pages/films/index.vue'

function filmNode(id: string, title: string) {
  return {
    id,
    title,
    description: null,
    releaseDate: '2020-01-01',
    thumbnailUrl: null,
    director: { id: 'd1', name: 'Dir', surname: 'Ector' },
    genres: [],
  }
}

function filmsPage(nodes: ReturnType<typeof filmNode>[], endCursor: string | null, hasNextPage: boolean) {
  return {
    data: {
      films: {
        nodes,
        pageInfo: { hasNextPage, hasPreviousPage: false, startCursor: null, endCursor },
      },
    },
  }
}

function graphqlError(message: string) {
  return { errors: [{ message }] }
}

let requestBodies: Array<{ variables: Record<string, unknown> }> = []

/**
 * `registerEndpoint`'s `once` handlers are served LIFO (its dispatcher
 * reverses the registered list and takes the first match), so registering in
 * the reverse of the desired chronological order makes them actually get
 * served in that order.
 */
function mockGraphqlResponses(...responses: unknown[]) {
  for (const response of [...responses].reverse()) {
    registerEndpoint('/api/graphql', {
      method: 'POST',
      handler: async (event) => {
        requestBodies.push(await readBody(event))
        return response
      },
      once: true,
    })
  }
}

function mockGraphqlPending(): { resolve: (value: unknown) => void } {
  let resolve: (value: unknown) => void = () => {}
  const pending = new Promise((r) => { resolve = r })
  registerEndpoint('/api/graphql', {
    method: 'POST',
    handler: async (event) => {
      requestBodies.push(await readBody(event))
      return pending
    },
    once: true,
  })
  return { resolve }
}

beforeEach(() => {
  requestBodies = []
  FakeIntersectionObserver.reset()
  vi.stubGlobal('IntersectionObserver', FakeIntersectionObserver)
})

describe('films list infinite scroll', () => {
  it('renders the sentinel when the first page has a next page', async () => {
    mockGraphqlResponses(filmsPage([filmNode('1', 'Film One')], 'cursor-1', true))

    const { container } = await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    expect(container.querySelector('[data-testid="films-sentinel"]')).not.toBeNull()
  })

  it('does not render the sentinel when there is no next page', async () => {
    mockGraphqlResponses(filmsPage([filmNode('1', 'Film One')], null, false))

    const { container } = await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    expect(container.querySelector('[data-testid="films-sentinel"]')).toBeNull()
  })

  it('intersecting the sentinel fetches the next page using the previous endCursor and appends the result', async () => {
    mockGraphqlResponses(
      filmsPage([filmNode('1', 'Film One')], 'cursor-1', true),
      filmsPage([filmNode('2', 'Film Two')], 'cursor-2', false),
    )

    await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    FakeIntersectionObserver.latest().trigger(true)
    await screen.findByText('Film Two')

    expect(screen.getByText('Film One')).toBeTruthy()
    expect(requestBodies).toHaveLength(2)
    expect(requestBodies[1]?.variables.after).toBe('cursor-1')
  })

  it('does not fetch a duplicate page when the sentinel intersects twice in quick succession', async () => {
    // Registered in reverse of chronological order -- see the `once`-ordering
    // note on `mockGraphqlResponses`/`mockGraphqlPending`.
    const second = mockGraphqlPending()
    mockGraphqlResponses(filmsPage([filmNode('1', 'Film One')], 'cursor-1', true))

    await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    FakeIntersectionObserver.latest().trigger(true)
    FakeIntersectionObserver.latest().trigger(true)
    await waitFor(() => expect(requestBodies).toHaveLength(2))

    second.resolve(filmsPage([filmNode('2', 'Film Two')], 'cursor-2', false))
    await screen.findByText('Film Two')
    expect(requestBodies).toHaveLength(2)
  })

  it('keeps already-loaded films visible and does not show the full-page loading text during a load-more fetch', async () => {
    // Registered in reverse of chronological order -- see the `once`-ordering
    // note on `mockGraphqlResponses`/`mockGraphqlPending`.
    const second = mockGraphqlPending()
    mockGraphqlResponses(filmsPage([filmNode('1', 'Film One')], 'cursor-1', true))

    await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    FakeIntersectionObserver.latest().trigger(true)
    await waitFor(() => expect(requestBodies).toHaveLength(2))

    expect(screen.getByText('Film One')).toBeTruthy()
    expect(screen.queryByText('Loading…')).toBeNull()

    second.resolve(filmsPage([filmNode('2', 'Film Two')], 'cursor-2', false))
    await screen.findByText('Film Two')
  })

  it('offers a retry control when a load-more fetch fails, and retrying loads the next page', async () => {
    mockGraphqlResponses(
      filmsPage([filmNode('1', 'Film One')], 'cursor-1', true),
      graphqlError('Network error'),
      filmsPage([filmNode('2', 'Film Two')], 'cursor-2', false),
    )

    await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    FakeIntersectionObserver.latest().trigger(true)
    await screen.findByText('Something went wrong loading more films.')
    expect(screen.getByText('Film One')).toBeTruthy()

    await fireEvent.click(screen.getByText('Retry'))
    await screen.findByText('Film Two')

    expect(screen.getByText('Film One')).toBeTruthy()
    expect(requestBodies).toHaveLength(3)
    expect(requestBodies[2]?.variables.after).toBe('cursor-1')
  })

  it('replaces the films and re-evaluates the sentinel when a new search returns a fresh first page', async () => {
    mockGraphqlResponses(
      filmsPage([filmNode('1', 'Film One')], 'cursor-1', true),
      filmsPage([filmNode('9', 'Searched Film')], null, false),
    )

    const { container } = await renderSuspended(FilmsIndexPage)
    await screen.findByText('Film One')

    await fireEvent.update(screen.getByPlaceholderText('Search by title'), { target: { value: 'Searched' } })
    await screen.findByText('Searched Film')

    expect(screen.queryByText('Film One')).toBeNull()
    expect(container.querySelector('[data-testid="films-sentinel"]')).toBeNull()
  })
})
