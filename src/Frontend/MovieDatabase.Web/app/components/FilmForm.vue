<script setup lang="ts">
import { z } from 'zod'
import type { FilmFormValue } from '../../lib/filmForm'
import { hydrateFormState, sanitizeFilmFormValue } from '../../lib/filmForm'
import ImageUploader from './ImageUploader.vue'

// Re-exported so Task 18's edit page (and anything else building on this
// form) can import the contract type directly from the component, e.g.
// `import type { FilmFormValue } from '../../components/FilmForm.vue'`,
// without needing to know it is actually declared in `lib/filmForm.ts`.
export type { FilmFormValue }

const props = defineProps<{
  initial?: FilmFormValue | null
  pending?: boolean
  submitLabel?: string
  /** Display-only: the film's existing thumbnail, from the fetched film on
   * the edit page. Absent on the create page, where there is nothing yet. */
  currentThumbnailUrl?: string | null
}>()

const emit = defineEmits<{ submit: [value: FilmFormValue] }>()

// Holds a newly uploaded image for this session only -- see
// `FilmFormValue.thumbnailId`'s doc comment in `lib/filmForm.ts` for why
// this is never hydrated from `initial`.
const pendingThumbnail = ref<{ id: string, url: string } | null>(null)

// A new upload always wins over the film's existing thumbnail for preview
// purposes, but only `pendingThumbnail.id` (never its `url`) is ever sent
// to the server -- `url` is just-uploaded-original-image URL, good enough
// to preview immediately without waiting on the async thumbnail pipeline.
const previewUrl = computed(() => pendingThumbnail.value?.url ?? props.currentThumbnailUrl ?? null)

function onThumbnailUploaded(blob: { id: string, fileName: string, url: string }) {
  pendingThumbnail.value = { id: blob.id, url: blob.url }
}

// `editFilm` overwrites every field it receives (`EditFilmRequestHandler`
// assigns `Title`/`Description`/`ReleaseDate`/`Director`/`Producer`/`Actors`/
// `Genres` unconditionally, with no partial patch) so `initial`, when given,
// must already be a complete `FilmFormValue` -- there is nothing here to
// fall back to per-field. A missing `initial` (the create page) starts every
// field blank instead.
const schema = z.object({
  title: z.string().min(1, 'Title is required'),
  releaseDate: z.string().min(1, 'Release date is required'),
  description: z.string(),
  director: z.object({
    name: z.string().min(1, 'Director first name is required'),
    surname: z.string().min(1, 'Director surname is required'),
  }),
  producer: z.object({
    name: z.string().min(1, 'Producer is required'),
  }),
})

// `hydrateFormState` (not inlined here) is what carries `initial`'s
// director/producer/actor/genre ids onto the editing state -- see its doc
// comment in `lib/filmForm.ts` for the bug that extraction fixed.
const state = reactive<FilmFormValue>(hydrateFormState(props.initial))

function addActor() {
  state.actors.push({ name: '', surname: '' })
}

function removeActor(index: number) {
  state.actors.splice(index, 1)
}

function addGenre() {
  state.genres.push({ name: '' })
}

function removeGenre(index: number) {
  state.genres.splice(index, 1)
}

function onSubmit() {
  emit('submit', { ...sanitizeFilmFormValue(state), thumbnailId: pendingThumbnail.value?.id })
}
</script>

<template>
  <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
    <div>
      <span class="mb-2 block text-sm font-medium">Cover image</span>
      <img
        v-if="previewUrl"
        :src="previewUrl"
        alt=""
        class="mb-2 aspect-video w-full max-w-xs rounded object-cover"
      >
      <div v-else class="mb-2 flex aspect-video w-full max-w-xs items-center justify-center rounded bg-gray-100 text-sm text-gray-400 dark:bg-gray-800 dark:text-gray-500">
        No image selected
      </div>
      <ImageUploader @uploaded="onThumbnailUploaded" />
    </div>

    <UFormField label="Title" name="title">
      <UInput v-model="state.title" class="w-full" />
    </UFormField>

    <UFormField label="Release date" name="releaseDate">
      <UInput v-model="state.releaseDate" type="date" class="w-full" />
    </UFormField>

    <UFormField label="Description" name="description">
      <UTextarea v-model="state.description" class="w-full" />
    </UFormField>

    <div class="grid gap-4 sm:grid-cols-2">
      <UFormField label="Director first name" name="director.name">
        <UInput v-model="state.director.name" class="w-full" />
      </UFormField>
      <UFormField label="Director surname" name="director.surname">
        <UInput v-model="state.director.surname" class="w-full" />
      </UFormField>
    </div>

    <UFormField label="Producer" name="producer.name">
      <UInput v-model="state.producer.name" class="w-full" />
    </UFormField>

    <div>
      <div class="mb-2 flex items-center justify-between">
        <span class="text-sm font-medium">Cast</span>
        <UButton size="xs" variant="ghost" @click="addActor">
          Add actor
        </UButton>
      </div>
      <div v-for="(actor, index) in state.actors" :key="index" class="mb-2 flex gap-2">
        <UInput v-model="actor.name" placeholder="First name" class="w-full" />
        <UInput v-model="actor.surname" placeholder="Surname" class="w-full" />
        <UButton size="xs" variant="ghost" color="error" @click="removeActor(index)">
          Remove
        </UButton>
      </div>
    </div>

    <div>
      <div class="mb-2 flex items-center justify-between">
        <span class="text-sm font-medium">Genres</span>
        <UButton size="xs" variant="ghost" @click="addGenre">
          Add genre
        </UButton>
      </div>
      <div v-for="(genre, index) in state.genres" :key="index" class="mb-2 flex gap-2">
        <UInput v-model="genre.name" placeholder="Genre" class="w-full" />
        <UButton size="xs" variant="ghost" color="error" @click="removeGenre(index)">
          Remove
        </UButton>
      </div>
    </div>

    <UButton type="submit" :loading="pending">
      {{ submitLabel ?? 'Save' }}
    </UButton>
  </UForm>
</template>
