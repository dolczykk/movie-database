<script setup lang="ts">
interface Person { id: string, name: string, surname?: string | null }
interface Genre { id: string, name: string }

const props = defineProps<{
  film: {
    id: string
    title: string
    description?: string | null
    releaseDate: string
    thumbnailUrl?: string | null
    director?: Person | null
    genres?: Genre[] | null
  }
}>()

const directorName = computed(() => {
  const director = props.film.director
  return director ? `${director.name} ${director.surname ?? ''}`.trim() : null
})
</script>

<template>
  <UCard>
    <img
      v-if="film.thumbnailUrl"
      :src="film.thumbnailUrl"
      :alt="film.title"
      class="mb-3 aspect-video w-full rounded object-cover"
    >
    <div v-else class="mb-3 flex aspect-video w-full items-center justify-center rounded bg-gray-100 text-sm text-gray-400 dark:bg-gray-800 dark:text-gray-500">
      No image available
    </div>

    <h2 class="font-semibold">
      <NuxtLink :to="`/films/${film.id}`">
        {{ film.title }}
      </NuxtLink>
    </h2>

    <p v-if="directorName" class="text-sm text-gray-500">
      Directed by {{ directorName }}
    </p>

    <p v-if="film.description" class="mt-2 line-clamp-3 text-sm">
      {{ film.description }}
    </p>

    <div v-if="film.genres?.length" class="mt-3 flex flex-wrap gap-1">
      <UBadge v-for="genre in film.genres" :key="genre.id" variant="subtle">
        {{ genre.name }}
      </UBadge>
    </div>
  </UCard>
</template>
