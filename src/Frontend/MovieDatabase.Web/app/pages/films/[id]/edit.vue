<script setup lang="ts">
import { DeleteFilmDocument, EditFilmDocument, GetFilmByIdDocument } from '../../../../graphql/generated/graphql'
import type { FilmFormValue } from '../../../components/FilmForm.vue'
import { selectFilm } from '../../../../lib/filmDetail'
import { toEditFilmInput, toFilmFormValue } from '../../../../lib/filmForm'
import { isValidFilmId } from '../../../../lib/filmId'
import { useAuthStore } from '../../../stores/auth'

// Editing (`editFilm`) authorizes Administrator OR Moderator on the API, so
// the page itself gates on the broader `manage-films` -- the narrower
// Administrator-only `deleteFilm` is instead gated on the delete button
// below (`store.isAdministrator`), not on the route.
definePageMeta({ middleware: 'manage-films' })

const route = useRoute()
const store = useAuthStore()
const id = route.params.id as string

// See the detail page (Task 16) for why a malformed id is checked, and
// reported as 404, before the API is ever queried.
if (!isValidFilmId(id)) {
  throw createError({ statusCode: 404, statusMessage: 'Film not found', fatal: true })
}

const pending = ref(false)
const error = ref<string | null>(null)

const { data, error: fetchError } = await useAsyncData(`film-edit-${id}`, () =>
  useGraphql(GetFilmByIdDocument, { id }))

// See the detail page (Task 16) for why a fetch failure is reported as a
// distinct 500 rather than falling through to the 404 below.
if (fetchError.value) {
  throw createError({ statusCode: 500, statusMessage: 'Failed to load film', fatal: true })
}

const film = computed(() => selectFilm(data.value))

if (!film.value) {
  throw createError({ statusCode: 404, statusMessage: 'Film not found', fatal: true })
}

// Captured as a plain value (rather than read again inside `initial` below)
// so TypeScript knows it is non-null -- the throw above only narrows this
// reference, not future reads of `film.value` from inside a closure.
const loadedFilm = film.value

// Carries director/producer/actor/genre ids from the fetched film onto the
// form value -- see `toFilmFormValue`'s doc comment for why that matters.
// `loadedFilm` never changes after load, so this needs no `computed`.
const initial = toFilmFormValue(loadedFilm)

async function onSubmit(value: FilmFormValue) {
  pending.value = true
  error.value = null
  try {
    // editFilm overwrites every field unconditionally, so this always sends
    // the complete film -- and `toEditFilmInput` carries the sub-document
    // ids through too, so unedited director/producer/actors/genres keep
    // their identity instead of being reissued as new ones.
    await useGraphql(EditFilmDocument, { input: toEditFilmInput(id, value) })
    await navigateTo(`/films/${id}`)
  }
  catch {
    error.value = 'The film could not be saved.'
  }
  finally {
    pending.value = false
  }
}

async function onDelete() {
  if (!confirm('Delete this film? This cannot be undone.')) {
    return
  }

  pending.value = true
  error.value = null
  try {
    await useGraphql(DeleteFilmDocument, { filmId: id })
    await navigateTo('/films')
  }
  catch {
    error.value = 'The film could not be deleted.'
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl">
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">
        Edit film
      </h1>
      <!-- deleteFilm is Administrator-only, unlike editFilm (Administrator
           OR Moderator, which is what the page's own `manage-films`
           middleware gates on) -- so this button is gated separately on the
           narrower `isAdministrator`, not the page's own access. -->
      <UButton v-if="store.isAdministrator" color="error" variant="outline" :loading="pending" @click="onDelete">
        Delete
      </UButton>
    </div>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <FilmForm :initial="initial" :pending="pending" submit-label="Save changes" @submit="onSubmit" />
  </div>
</template>
