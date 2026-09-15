export default defineNuxtConfig({
  compatibilityDate: '2026-09-11',
  devtools: { enabled: true },
  modules: ['@nuxt/ui', '@pinia/nuxt'],
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    // Server-only. Overridden by NUXT_GRAPHQL_ENDPOINT, which Aspire injects (Task 20).
    graphqlEndpoint: 'https://localhost:7204/graphql',
  },
})
