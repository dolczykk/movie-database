<script setup lang="ts">
// Default title for every page; a page that sets its own (e.g. the film
// detail page via `useSeoMeta`) has it composed in rather than replaced.
useHead({
  titleTemplate: title => title ? `${title} - Movie Database` : 'Movie Database',
})
</script>

<template>
  <UApp>
    <NuxtRouteAnnouncer />
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </UApp>
</template>
