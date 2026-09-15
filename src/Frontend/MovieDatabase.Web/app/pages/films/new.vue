<script setup lang="ts">
import { CreateFilmDocument } from '../../../graphql/generated/graphql'
import type { FilmFormValue } from '../../components/FilmForm.vue'
import { describeCreateFilmError } from '../../../lib/createFilmError'

// `createFilm` is Administrator-only on the API (`[Authorize(Roles =
// [Administrator])]`), so this page gates on the narrower `admin`
// middleware rather than `manage-films` (Administrator OR Moderator, which
// only covers `editFilm` -- Task 18's edit page uses that one).
definePageMeta({ middleware: 'admin' })

const pending = ref(false)
const error = ref<string | null>(null)

async function onSubmit(value: FilmFormValue) {
  pending.value = true
  error.value = null
  try {
    const result = await useGraphql(CreateFilmDocument, {
      input: {
        title: value.title,
        releaseDate: value.releaseDate,
        description: value.description,
        director: { name: value.director.name, surname: value.director.surname },
        producer: { name: value.producer.name },
        actors: value.actors,
        genres: value.genres,
      },
    })
    await navigateTo(`/films/${result.createFilm.id}`)
  }
  catch (caught) {
    error.value = describeCreateFilmError(caught)
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <div class="max-w-2xl">
    <h1 class="mb-4 text-xl font-semibold">
      Add film
    </h1>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <FilmForm :pending="pending" submit-label="Create film" @submit="onSubmit" />
  </div>
</template>
