<script setup lang="ts">
import { GetFilmByIdDocument } from '../../../../graphql/generated/graphql'
import { selectFilm } from '../../../../lib/filmDetail'
import { isValidFilmId } from '../../../../lib/filmId'
import { useAuthStore } from '../../../stores/auth'

const route = useRoute()
const store = useAuthStore()
const id = route.params.id as string

// `GetFilmById($id: UUID!)` means a non-UUID param fails scalar coercion on
// the API and comes back as a GraphQL error -- indistinguishable, below,
// from a genuine backend outage. Checked before the request is even made so
// a malformed id (a typo, crawler noise, ...) reports as 404, as a third
// case alongside -- not instead of -- the outage/not-found distinction the
// next two checks make.
if (!isValidFilmId(id)) {
  throw createError({ statusCode: 404, statusMessage: 'Film not found', fatal: true })
}

const { data, error } = await useAsyncData(`film-${id}`, () =>
  useGraphql(GetFilmByIdDocument, { id }))

// `useAsyncData` swallows the handler's throw into `error` rather than
// rethrowing it, so a network failure, a backend 500, or a GraphQL `errors`
// payload from `runGraphql` all leave `data.value` null -- the same shape
// `selectFilm` sees for a genuinely missing film. Checked first, and kept
// as a distinct 500 rather than falling through to the 404 below, or a
// real outage gets misreported as "Film not found" instead of a backend
// failure. Mirrors the list page's `error`-before-empty ordering (Task 15).
if (error.value) {
  throw createError({ statusCode: 500, statusMessage: 'Failed to load film', fatal: true })
}

// The API has no single-film resolver: `GetFilmById` filters the paginated
// `films` field to (at most) one node, and a nonexistent id comes back as an
// empty `nodes` array rather than a GraphQL error. `selectFilm` collapses
// every "no match" shape (empty nodes, null nodes, null films) to `null`.
const film = computed(() => selectFilm(data.value))

if (!film.value) {
  throw createError({ statusCode: 404, statusMessage: 'Film not found', fatal: true })
}

const people = computed(() => film.value?.actors ?? [])
const genres = computed(() => film.value?.genres ?? [])

// Film pages are public content -- SEO/first-paint is this route's whole
// reason for being server-rendered (see the root README), so its own title
// and description are worth setting from the film's real values.
useSeoMeta({
  title: () => film.value?.title,
  description: () => film.value?.description ?? undefined,
})
</script>

<template>
  <article v-if="film">
    <div class="mb-4 flex items-start justify-between gap-4">
      <div>
        <h1 class="text-2xl font-semibold">
          {{ film.title }}
        </h1>
        <p class="text-sm text-gray-500">
          Released {{ film.releaseDate }}
        </p>
      </div>
      <!-- Editing (`editFilm`) authorizes Administrator OR Moderator, unlike
           the list page's Administrator-only "Add film" (`createFilm`), so
           this gates on the broader `canManageFilms` rather than
           `isAdministrator`. -->
      <div v-if="store.canManageFilms" class="flex gap-2">
        <UButton :to="`/films/${film.id}/edit`" variant="outline">
          Edit
        </UButton>
      </div>
    </div>

    <!-- No mutation in the API can currently set a film's thumbnail, so
         every film renders through this placeholder in practice -- it is
         the deliberate, normal appearance of this page, not a loading or
         error state. -->
    <img
      v-if="film.thumbnailUrl"
      :src="film.thumbnailUrl"
      :alt="film.title"
      class="mb-6 aspect-video w-full max-w-2xl rounded object-cover"
    >
    <div
      v-else
      class="mb-6 flex aspect-video w-full max-w-2xl items-center justify-center rounded bg-gray-100 text-sm text-gray-400 dark:bg-gray-800 dark:text-gray-500"
    >
      No image available
    </div>

    <p v-if="film.description" class="mb-6 max-w-2xl">
      {{ film.description }}
    </p>

    <dl class="grid gap-4 sm:grid-cols-2">
      <div v-if="film.director">
        <dt class="text-sm font-medium text-gray-500">
          Director
        </dt>
        <dd>{{ film.director.name }} {{ film.director.surname }}</dd>
      </div>

      <div v-if="film.producer">
        <dt class="text-sm font-medium text-gray-500">
          Producer
        </dt>
        <dd>{{ film.producer.name }}</dd>
      </div>

      <div v-if="people.length">
        <dt class="text-sm font-medium text-gray-500">
          Cast
        </dt>
        <dd>
          <ul>
            <li v-for="actor in people" :key="actor.id">
              {{ actor.name }} {{ actor.surname }}
            </li>
          </ul>
        </dd>
      </div>

      <div v-if="genres.length">
        <dt class="text-sm font-medium text-gray-500">
          Genres
        </dt>
        <dd class="flex flex-wrap gap-1">
          <UBadge v-for="genre in genres" :key="genre.id" variant="subtle">
            {{ genre.name }}
          </UBadge>
        </dd>
      </div>
    </dl>
  </article>
</template>
