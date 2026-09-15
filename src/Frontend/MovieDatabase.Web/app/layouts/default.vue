<script setup lang="ts">
import { useAuthStore } from '../stores/auth'

const store = useAuthStore()

async function onLogout() {
  await store.logout()
  await navigateTo('/login')
}
</script>

<template>
  <div class="min-h-screen">
    <header class="border-b">
      <nav class="mx-auto flex max-w-5xl items-center justify-between p-4">
        <NuxtLink to="/films" class="font-semibold">
          Movie Database
        </NuxtLink>
        <div class="flex items-center gap-3">
          <template v-if="store.isAuthenticated">
            <span class="text-sm">{{ store.user?.username }}</span>
            <UButton variant="ghost" @click="onLogout">
              Log out
            </UButton>
          </template>
          <template v-else>
            <UButton to="/login" variant="ghost">
              Log in
            </UButton>
            <UButton to="/register">
              Register
            </UButton>
          </template>
        </div>
      </nav>
    </header>
    <main class="mx-auto max-w-5xl p-4">
      <slot />
    </main>
  </div>
</template>
