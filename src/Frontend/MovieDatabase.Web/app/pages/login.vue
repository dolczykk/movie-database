<script setup lang="ts">
import { z } from 'zod'
import { resolveLoginRedirect } from '../../lib/loginRedirect'
import { useAuthStore } from '../stores/auth'

const schema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(1, 'Enter your password'),
})

const state = reactive({ email: '', password: '' })
const error = ref<string | null>(null)
const pending = ref(false)

const store = useAuthStore()
const route = useRoute()

async function onSubmit() {
  error.value = null
  pending.value = true
  try {
    await store.login(state.email, state.password)
    // `route.query.redirect` is attacker-controllable (see resolveLoginRedirect
    // for why); it is never navigated to directly.
    await navigateTo(resolveLoginRedirect(route.query.redirect))
  }
  catch {
    error.value = 'Those credentials were not accepted.'
  }
  finally {
    pending.value = false
  }
}
</script>

<template>
  <UCard class="mx-auto max-w-md">
    <template #header>
      <h1 class="text-lg font-semibold">
        Log in
      </h1>
    </template>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
      <UFormField label="Email" name="email">
        <UInput v-model="state.email" type="email" autocomplete="email" class="w-full" />
      </UFormField>

      <UFormField label="Password" name="password">
        <UInput v-model="state.password" type="password" autocomplete="current-password" class="w-full" />
      </UFormField>

      <UButton type="submit" :loading="pending" block>
        Log in
      </UButton>
    </UForm>
  </UCard>
</template>
