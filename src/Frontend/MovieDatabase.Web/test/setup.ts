import { config } from '@vue/test-utils'
import '@testing-library/vue'

// Vitest runs under plain `happy-dom`, not Nuxt's `environment: 'nuxt'`, so
// NuxtUI components (which call `useNuxtApp` internally) and `NuxtLink`
// cannot resolve on their own. Stub them globally for any component test
// that renders markup using them.
config.global.stubs = {
  UCard: { template: '<div><slot /></div>' },
  UBadge: { template: '<span><slot /></span>' },
  UButton: { template: '<button><slot /></button>' },
  UInput: { template: '<input>' },
  UTextarea: { template: '<textarea></textarea>' },
  UFormField: { template: '<div><slot /></div>' },
  // Real UForm runs zod validation before emitting `submit`; stubbed as a
  // plain form so FilmForm's own `onSubmit` wiring can be tested without
  // needing every field to satisfy the schema.
  UForm: { template: '<form @submit.prevent="$emit(\'submit\')"><slot /></form>' },
  NuxtLink: { template: '<a><slot /></a>' },
}
