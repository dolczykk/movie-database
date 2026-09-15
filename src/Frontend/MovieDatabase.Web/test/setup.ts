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
  NuxtLink: { template: '<a><slot /></a>' },
}
