import { createError } from 'h3'
import { readAuthCookies } from '../utils/authCookies'
import { postGraphqlRaw, type GraphqlRawRequest } from '../utils/graphqlClient'

export default defineEventHandler(async (event) => {
  const body = await readBody<GraphqlRawRequest>(event)
  const { accessToken } = readAuthCookies(event)

  try {
    // A successful HTTP response is returned untouched below, GraphQL
    // `errors` array included — the browser must see normal GraphQL errors.
    // Only a transport-level failure (network error, non-2xx response) is
    // caught here, and must not leak the upstream endpoint or body to the
    // client.
    return await postGraphqlRaw(event, body, accessToken)
  }
  catch (error) {
    // Logs the message only, never the raw error object. ofetch attaches
    // `request`/`options`/`response` (this request's `Authorization` header
    // included) as non-enumerable properties, so the inspector doesn't
    // print them today -- but that's an internal detail of a transitive,
    // unpinned dependency, not something to rely on for a secret.
    console.error('[api/graphql] upstream request failed', error instanceof Error ? error.message : String(error))
    throw createError({ statusCode: 502, statusMessage: 'Unable to reach the GraphQL API' })
  }
})
