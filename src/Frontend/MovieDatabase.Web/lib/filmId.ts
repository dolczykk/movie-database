const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/**
 * Mirrors the shape HotChocolate's `UUID` scalar accepts for
 * `GetFilmById($id: UUID!)`.
 *
 * A route param that isn't shaped like a UUID (a typo, crawler noise, an
 * old numeric id scheme, ...) would otherwise reach the API, fail scalar
 * coercion, and come back as a GraphQL error -- which `films/[id]/index.vue`
 * and `edit.vue` cannot tell apart from a genuine backend outage, so it
 * gets reported as a 500. Checking the shape first turns that into a third,
 * distinct case (malformed id -> 404) without touching the existing
 * outage-vs-missing distinction those pages already make: a well-formed id
 * still goes to the API and can still fail as a real 500, or resolve to no
 * film as a 404 via the empty-`nodes` case.
 */
export function isValidFilmId(id: string): boolean {
  return UUID_PATTERN.test(id)
}
