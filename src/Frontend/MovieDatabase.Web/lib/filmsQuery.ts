import type { FilmFilterInput, FilmSortInput } from '../graphql/generated/graphql'

/** Must stay at or below the API's `Constants.Query.MaxPageSize` of 20. */
export const FILMS_PAGE_SIZE = 12

export const FILMS_DEFAULT_ORDER: FilmSortInput[] = [{ title: 'ASC' }]

/**
 * Builds the `where` filter for the films list query from the raw search
 * box input.
 *
 * - A blank or whitespace-only search collapses to `null` (no filter)
 *   rather than sending `{ title: { contains: '' } }` — functionally the
 *   same match-everything result, but an unnecessary filter round trip.
 * - The term is trimmed so incidental leading/trailing whitespace from the
 *   input box does not narrow the match.
 */
export function buildFilmsWhere(search: string): FilmFilterInput | null {
  const trimmed = search.trim()
  return trimmed ? { title: { contains: trimmed } } : null
}

/**
 * Decides the cursor to request next when "Load more" is activated.
 *
 * Paging is Relay-style cursor-based: the only valid way to advance is the
 * server-supplied `endCursor`, never offset math. Falls back to `null`
 * (first page) when the cursor is missing, which should not happen while
 * `hasNextPage` is true, but keeps this total rather than partial.
 */
export function nextFilmsCursor(endCursor: string | null | undefined): string | null {
  return endCursor ?? null
}

/**
 * Decides how a freshly-fetched page of film nodes combines with what is
 * already accumulated on screen.
 *
 * `after` is the cursor that produced `incoming` -- i.e. the `after`
 * variable sent with the request, not the *next* cursor to request.
 *
 * - `after === null` means this was a first-page fetch: either the initial
 *   load, or a search that just reset the cursor. Either way, `incoming`
 *   replaces the accumulation outright rather than merging with it -- a new
 *   search must not show films left over from a previous one, and an empty
 *   result must clear the grid rather than leave stale films visible.
 * - Any other `after` means this was a "Load more" fetch of a later page,
 *   so `incoming` is appended, which is what the "Load more" control
 *   promises: growing the list rather than replacing it.
 */
export function accumulateFilmNodes<T>(previous: T[], incoming: T[], after: string | null): T[] {
  return after === null ? incoming : [...previous, ...incoming]
}
