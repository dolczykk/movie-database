import { guardDecision } from '../../lib/guardDecision'
import { useAuthStore } from '../stores/auth'

// UX guard, not access control: the store's role comes from a JWT the server
// routes decode but never verify (that would need the API's signing key in a
// second place). A crafted cookie can pass this check; real authorization
// happens on the .NET API, which does verify signatures.
export default defineNuxtRouteMiddleware((to) => {
  const store = useAuthStore()
  const decision = guardDecision(store.isAuthenticated, store.isAuthenticated, to.fullPath)

  if (decision.type === 'redirect') {
    return navigateTo(decision.to)
  }

  if (decision.type === 'forbid') {
    throw createError({ statusCode: 403, statusMessage: 'Forbidden' })
  }
})
