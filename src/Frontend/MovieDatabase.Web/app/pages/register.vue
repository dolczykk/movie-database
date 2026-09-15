<script setup lang="ts">
import { z } from 'zod'
import { useAuthStore } from '../stores/auth'

const schema = z.object({
  username: z.string().min(1, 'Choose a username'),
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(8, 'Use at least 8 characters'),
})

const state = reactive({ username: '', email: '', password: '' })
const error = ref<string | null>(null)
const pending = ref(false)

const store = useAuthStore()

async function onSubmit() {
  error.value = null
  pending.value = true
  try {
    await store.register(state.username, state.email, state.password)
    await navigateTo('/films')
  }
  catch {
    // Deliberately generic: whether registration failed because the email is
    // already taken, validation rejected the input, or the API was
    // unreachable, the visitor sees the same message either way. Branching
    // this on the underlying error would let an attacker enumerate which
    // emails are already registered.
    error.value = 'That account could not be created. The email may already be registered.'
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
        Create an account
      </h1>
    </template>

    <UAlert v-if="error" color="error" :description="error" class="mb-4" />

    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
      <UFormField label="Username" name="username">
        <UInput v-model="state.username" autocomplete="username" />
      </UFormField>

      <UFormField label="Email" name="email">
        <UInput v-model="state.email" type="email" autocomplete="email" />
      </UFormField>

      <UFormField label="Password" name="password">
        <UInput v-model="state.password" type="password" autocomplete="new-password" />
      </UFormField>

      <UButton type="submit" :loading="pending" block>
        Register
      </UButton>
    </UForm>
  </UCard>
</template>
