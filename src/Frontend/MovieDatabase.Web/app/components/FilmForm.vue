<script setup lang="ts">
import { z } from 'zod'
import type { FilmFormValue } from '../../lib/filmForm'
import { hydrateFormState, sanitizeFilmFormValue } from '../../lib/filmForm'

// Re-exported so Task 18's edit page (and anything else building on this
// form) can import the contract type directly from the component, e.g.
// `import type { FilmFormValue } from '../../components/FilmForm.vue'`,
// without needing to know it is actually declared in `lib/filmForm.ts`.
export type { FilmFormValue }

const props = defineProps<{
  initial?: FilmFormValue | null
  pending?: boolean
  submitLabel?: string
}>()

const emit = defineEmits<{ submit: [value: FilmFormValue] }>()

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
  emit('submit', sanitizeFilmFormValue(state))
}
</script>

<template>
  <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
    <UFormField label="Title" name="title">
      <UInput v-model="state.title" />
    </UFormField>

    <UFormField label="Release date" name="releaseDate">
      <UInput v-model="state.releaseDate" type="date" />
    </UFormField>

    <UFormField label="Description" name="description">
      <UTextarea v-model="state.description" />
    </UFormField>

    <div class="grid gap-4 sm:grid-cols-2">
      <UFormField label="Director first name" name="director.name">
        <UInput v-model="state.director.name" />
      </UFormField>
      <UFormField label="Director surname" name="director.surname">
        <UInput v-model="state.director.surname" />
      </UFormField>
    </div>

    <UFormField label="Producer" name="producer.name">
      <UInput v-model="state.producer.name" />
    </UFormField>

    <div>
      <div class="mb-2 flex items-center justify-between">
        <span class="text-sm font-medium">Cast</span>
        <UButton size="xs" variant="ghost" @click="addActor">
          Add actor
        </UButton>
      </div>
      <div v-for="(actor, index) in state.actors" :key="index" class="mb-2 flex gap-2">
        <UInput v-model="actor.name" placeholder="First name" />
        <UInput v-model="actor.surname" placeholder="Surname" />
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
        <UInput v-model="genre.name" placeholder="Genre" />
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
