/**
 * Turns a failed `createFilm` mutation into a message the visitor can act
 * on.
 *
 * `CreateFilmRequestHandler` throws `FilmExistsApplicationException`
 * ("Film with the same title exists") when a film with the same title
 * already exists -- a mistake the visitor can fix by choosing a different
 * title, unlike every other failure (a network error, a backend 500,
 * validation the client-side schema did not already catch), which is
 * reported with a generic message instead.
 *
 * `useGraphql`/`runGraphql` surface a GraphQL `errors[0].message` as the
 * `message` of the `H3Error` they throw (`createError({ statusMessage })`,
 * and h3's `H3Error` falls a missing `message` back to `statusMessage`), so
 * the duplicate-title case is detected by matching on the exception's own
 * wording rather than a status code -- HotChocolate does not distinguish
 * application exceptions from one another by status code.
 */
export function describeCreateFilmError(error: unknown): string {
  if (error instanceof Error && error.message.includes('same title')) {
    return 'A film with that title already exists. Choose a different title.'
  }

  return 'The film could not be created.'
}
