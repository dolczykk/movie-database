import type { EditFilmInput, GetFilmByIdQuery } from '../graphql/generated/graphql'

/**
 * The value `FilmForm` emits on submit, and the shape `createFilm`/`editFilm`
 * are built from.
 *
 * This is always a *complete* film, never a diff: `EditFilmRequestHandler`
 * assigns `Title`, `Description`, `ReleaseDate`, `Director`, `Producer`,
 * `Actors` and `Genres` unconditionally from whatever `EditFilmInput` it
 * receives, so a caller that sent only changed fields would silently blank
 * out the rest. There is deliberately no top-level `id` here -- both the
 * create page (Task 17) and the edit page (Task 18) hold the film's own id
 * outside the form (a route param for edit, nonexistent for create) and
 * attach it to the mutation input themselves.
 *
 * The sub-document `id`s (director/producer/actor/genre) are different: they
 * *do* belong on the form value, because `EditFilmRequestHandler` does
 * `Id != null ? Guid.Parse(request...Id) : Guid.NewGuid()` for each one --
 * omitting an id on an edit mints a brand-new id for that sub-document on
 * every save, silently breaking anything that referenced the old one (the
 * API exposes these through its `actors`/`genres`/`directors`/`producers`
 * queries). They are optional because the create page's new film has no
 * sub-document ids yet -- `toFilmFormValue` below is what populates them,
 * for the edit page only.
 *
 * `thumbnailId` is the one field that breaks the "always a complete film"
 * rule above, and is never populated by `toFilmFormValue`: the raw blob id
 * backing a film's thumbnail is intentionally never exposed over GraphQL
 * (only the computed, display-only `thumbnailUrl` is), so there is nothing
 * to hydrate it from. It is set exactly once, by `FilmForm`'s own
 * `ImageUploader` integration, only when the visitor picks a new image
 * during the current session. `undefined` always means "no new image was
 * picked this session" -- for create that means no thumbnail at all; for
 * edit it specifically does NOT mean "clear the thumbnail"
 * (`EditFilmRequestHandler` preserves the existing one when `thumbnail` is
 * omitted/null), since there is no remove-thumbnail affordance in the UI.
 */
export interface FilmFormValue {
  title: string
  releaseDate: string
  description: string
  director: { id?: string, name: string, surname: string }
  producer: { id?: string, name: string }
  actors: Array<{ id?: string, name: string, surname: string }>
  genres: Array<{ id?: string, name: string }>
  thumbnailId?: string
}

/**
 * Shapes the form's live editing state into the value `FilmForm` emits on
 * submit.
 *
 * "Add actor"/"Add genre" append a blank row for the visitor to fill in;
 * `CreateFilmRequestHandler`/`EditFilmRequestHandler` map `Actors`/`Genres`
 * onto the film verbatim with no server-side trimming or validation, so a
 * row left blank (never filled in, or added and then abandoned) would
 * otherwise reach the API as an actor/genre placeholder with an empty name.
 * This drops any such row before the value leaves the form.
 *
 * A row counts as blank only by its `name` -- an actor with a name but a
 * blank surname is kept, since surname is optional in the schema
 * (`ActorPlaceholderInput.surname: String`, not `String!`).
 */
export function sanitizeFilmFormValue(value: FilmFormValue): FilmFormValue {
  return {
    ...value,
    actors: value.actors.filter(actor => actor.name.trim() !== ''),
    genres: value.genres.filter(genre => genre.name.trim() !== ''),
  }
}

/**
 * Builds `FilmForm`'s reactive editing state from its `initial` prop.
 *
 * This is the hop between a fetched film's shaped `FilmFormValue`
 * (`toFilmFormValue`) and the value the visitor eventually submits (which
 * feeds `toEditFilmInput`) -- and the one place a real bug shipped during
 * this task's own self-review: `director`/`producer` were rebuilt by
 * destructuring `{ name, surname }`/`{ name }` straight off `initial`,
 * silently dropping whatever `id` it carried (`actors`/`genres` were safe
 * because they were already spread whole). Extracted to a pure function so
 * that mistake has a test that would catch it -- see
 * `test/lib/filmForm.spec.ts`'s `hydrateFormState` suite.
 *
 * `initial` is `null`/`undefined` for the create page, where there is
 * nothing to hydrate from: every field, ids included, legitimately starts
 * blank/absent rather than some placeholder value.
 *
 * Returns fresh objects/arrays throughout (never `initial`'s own
 * references), so editing the resulting reactive state can never mutate
 * whatever the caller passed in as `initial`.
 */
export function hydrateFormState(initial: FilmFormValue | null | undefined): FilmFormValue {
  return {
    title: initial?.title ?? '',
    releaseDate: initial?.releaseDate ?? '',
    description: initial?.description ?? '',
    director: {
      id: initial?.director?.id,
      name: initial?.director?.name ?? '',
      surname: initial?.director?.surname ?? '',
    },
    producer: {
      id: initial?.producer?.id,
      name: initial?.producer?.name ?? '',
    },
    actors: initial?.actors ? initial.actors.map(actor => ({ ...actor })) : [],
    genres: initial?.genres ? initial.genres.map(genre => ({ ...genre })) : [],
  }
}

/** The single film node `GetFilmById` returns, exactly as the edit page reads it. */
type FetchedFilm = NonNullable<NonNullable<GetFilmByIdQuery['films']>['nodes']>[number]

/**
 * Shapes a fetched film into the value `FilmForm`'s `initial` prop expects,
 * carrying the director/producer/actor/genre ids along with their names.
 *
 * `GetFilmByIdDocument` already selects these ids; this is the one place
 * that copies them onto the form value so the edit page does not have to
 * reconstruct the mapping (and risk dropping an id) inline.
 */
export function toFilmFormValue(film: FetchedFilm): FilmFormValue {
  return {
    title: film.title,
    releaseDate: film.releaseDate,
    description: film.description ?? '',
    director: { id: film.director.id, name: film.director.name, surname: film.director.surname },
    producer: { id: film.producer.id, name: film.producer.name },
    actors: film.actors.map(actor => ({ id: actor.id, name: actor.name, surname: actor.surname })),
    genres: film.genres.map(genre => ({ id: genre.id, name: genre.name })),
  }
}

/**
 * Builds the `editFilm` mutation input from a submitted form value.
 *
 * Carries each sub-document's `id` (when the form value has one) straight
 * through, so a save keeps the director/producer/actor/genre identities
 * `toFilmFormValue` populated from the fetched film instead of
 * `EditFilmRequestHandler` minting fresh ones for them. A row with no id --
 * a newly added actor/genre, or (defensively) a create-page value passed
 * through here -- carries `id: undefined`, which the handler treats the same
 * as an absent id.
 *
 * Takes the already-submitted `FilmFormValue` as-is rather than
 * re-sanitizing it: `FilmForm` calls `sanitizeFilmFormValue` itself before
 * emitting, so by the time a page's `onSubmit` (and thus this function) sees
 * the value, blank actor/genre rows are already dropped.
 */
export function toEditFilmInput(id: string, value: FilmFormValue): EditFilmInput {
  return {
    id,
    title: value.title,
    releaseDate: value.releaseDate,
    description: value.description,
    director: { id: value.director.id, name: value.director.name, surname: value.director.surname },
    producer: { id: value.producer.id, name: value.producer.name },
    actors: value.actors.map(actor => ({ id: actor.id, name: actor.name, surname: actor.surname })),
    genres: value.genres.map(genre => ({ id: genre.id, name: genre.name })),
    thumbnail: value.thumbnailId ?? null,
  }
}
