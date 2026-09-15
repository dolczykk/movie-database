import type { H3Event } from 'h3'
import { createError } from 'h3'

interface GraphqlEnvelope<T> {
  data?: T
  errors?: Array<{ message: string }>
}

export interface GraphqlRawRequest {
  query: string
  variables?: Record<string, unknown>
  operationName?: string
}

/**
 * Forwards a GraphQL-over-HTTP request to the API and returns the upstream
 * envelope untouched (including an `errors` array) — unlike `callGraphql`,
 * this never inspects or throws on GraphQL-level errors. Used by the
 * browser-facing proxy route, which must let the client see normal GraphQL
 * errors rather than have them collapsed into an h3 error.
 *
 * A transport failure (network error, non-2xx HTTP response) still rejects
 * — the caller is responsible for not leaking that rejection's endpoint URL
 * or upstream body back to the browser.
 */
export async function postGraphqlRaw(
  event: H3Event,
  body: GraphqlRawRequest,
  accessToken?: string | null,
): Promise<unknown> {
  const endpoint = useRuntimeConfig(event).graphqlEndpoint

  return await $fetch(endpoint, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
    },
    body,
  })
}

export async function callGraphql<T>(
  event: H3Event,
  query: string,
  variables: Record<string, unknown>,
  accessToken?: string | null,
): Promise<T> {
  const endpoint = useRuntimeConfig(event).graphqlEndpoint

  const response = await $fetch<GraphqlEnvelope<T>>(endpoint, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
    },
    body: { query, variables },
  })

  if (response.errors?.length) {
    throw createError({
      statusCode: 400,
      statusMessage: response.errors[0]!.message,
    })
  }

  if (!response.data) {
    throw createError({ statusCode: 502, statusMessage: 'Empty GraphQL response' })
  }

  return response.data
}
