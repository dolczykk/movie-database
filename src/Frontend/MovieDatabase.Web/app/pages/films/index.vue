<script setup lang="ts">
import type { GetFilmsQuery } from '../../../graphql/generated/graphql'
import { GetFilmsDocument } from '../../../graphql/generated/graphql'
import { accumulateFilmNodes, buildFilmsWhere, FILMS_DEFAULT_ORDER, FILMS_PAGE_SIZE, nextFilmsCursor } from '../../../lib/filmsQuery'
import { useAuthStore } from '../../stores/auth'

type FilmNode = NonNullable<NonNullable<GetFilmsQuery['films']>['nodes']>[number]

// Creating a film is Administrator-only on the API (`createFilm` carries
// `[Authorize(Roles = [Administrator])]`), so the control is gated on
// `isAdministrator`, not `canManageFilms` (Administrator OR Moderator, which
// only covers `editFilm`) -- gating on the broader capability would show
// Moderators a control that 403s server-side.
const store = useAuthStore()
const search = ref('')
const cursor = ref<string | null>(null)

// `watch`/`refresh()` are deliberately not combined: this page drives every
// refetch explicitly (the debounced search handler and `loadMore` below both
// call `refresh()` themselves) rather than also listing `cursor` in
// `useAsyncData`'s `watch` option, which would fire its own independent
// refetch on top of the explicit one and race/duplicate it.
const { data, pending, error, refresh } = await useAsyncData(
  'films',
  () => useGraphql(GetFilmsDocument, {
    first: FILMS_PAGE_SIZE,
    after: cursor.value,
    where: buildFilmsWhere(search.value),
    order: FILMS_DEFAULT_ORDER,
  }),
)

const pageInfo = computed(() => data.value?.films?.pageInfo)

// `useGraphql`/`useAsyncData` only ever hold the latest single page, so the
// list shown on screen is accumulated here across fetches. `cursor.value` at
// the time each fetch resolves is the `after` that produced it: `null` means
// a first-page fetch (initial load, or a new/cleared search) and resets the
// accumulation outright; anything else means a "Load more" fetch and appends
// to it. See `accumulateFilmNodes` for why replace-vs-append is decided this
// way.
const films = ref<FilmNode[]>([])
watch(data, (latest) => {
  films.value = accumulateFilmNodes(films.value, latest?.films?.nodes ?? [], cursor.value)
}, { immediate: true })

async function loadMore() {
  cursor.value = nextFilmsCursor(pageInfo.value?.endCursor)
  await refresh()
}

// Debounced so every keystroke doesn't fire a request; a fresh search always
// restarts paging from the first page.
let debounce: ReturnType<typeof setTimeout>
watch(search, () => {
  clearTimeout(debounce)
  debounce = setTimeout(() => {
    cursor.value = null
    void refresh()
  }, 300)
})
</script>

<template>
  <div>
    <div class="mb-4 flex items-center justify-between gap-4">
      <h1 class="text-xl font-semibold">
        Films
      </h1>
      <div class="flex items-center gap-2">
        <UInput v-model="search" placeholder="Search by title" />
        <UButton v-if="store.isAdministrator" to="/films/new">
          Add film
        </UButton>
      </div>
    </div>

    <div v-if="pending" class="py-8 text-center text-sm text-gray-500">
      Loading…
    </div>

    <div v-else-if="error" class="py-8 text-center text-sm text-red-500">
      Something went wrong loading films. Please try again shortly.
    </div>

    <div v-else-if="!films.length" class="py-8 text-center text-sm text-gray-500">
      No films found.
    </div>

    <div v-else class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <FilmCard v-for="film in films" :key="film.id" :film="film" />
    </div>

    <div v-if="pageInfo?.hasNextPage" class="mt-6 text-center">
      <UButton variant="outline" @click="loadMore">
        Load more
      </UButton>
    </div>
  </div>
</template>
