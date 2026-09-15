/**
 * Shape of the `GetFilmById` connection this function reads from. Kept
 * generic (rather than importing `GetFilmByIdQuery` directly) so any
 * single-node connection result can be narrowed the same way, and so the
 * node type is inferred from whatever caller-supplied result is passed in.
 */
export interface FilmConnectionResult<TNode> {
  films?: {
    nodes?: TNode[] | null
  } | null
}

/**
 * Extracts the single film from the `GetFilmById` connection shape.
 *
 * The API has no single-film resolver, so `GetFilmById` filters the
 * paginated `films` field down to (at most) one node via `first: 1` and
 * `where: { id: { eq: $id } }`. A request for an id that does not exist
 * returns an empty `nodes` array rather than an error, so "no matching
 * film" and "malformed/missing connection wrapper" both collapse to the
 * same `null` here -- the caller (the page) treats a `null` result as the
 * signal to 404.
 */
export function selectFilm<TNode>(data: FilmConnectionResult<TNode> | null | undefined): TNode | null {
  return data?.films?.nodes?.[0] ?? null
}
