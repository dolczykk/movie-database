import type { DocumentNode } from 'graphql'
import type { TypedDocumentNode } from '@graphql-typed-document-node/core'
import { print } from 'graphql'

export interface GraphqlEnvelope<T> {
  data?: T
  errors?: Array<{ message: string }>
}

// `print(document)` re-serialises a document codegen already knows the
// source text of, and does it on every request -- shipping the graphql-js
// parser/printer to the browser to redo work that could be done once.
// Memoised per document (identity, not content) so repeated calls with the
// same generated `DocumentNode` reuse the printed string.
const printedDocuments = new WeakMap<DocumentNode, string>()

function printDocument(document: DocumentNode): string {
  let printed = printedDocuments.get(document)
  if (printed === undefined) {
    printed = print(document)
    printedDocuments.set(document, printed)
  }
  return printed
}

/**
 * Posts a printed GraphQL document to the same-origin proxy (`/api/graphql`)
 * and returns its `data`, throwing if the response carries a GraphQL
 * `errors` array.
 *
 * `headers` is taken as a plain argument — rather than this function reading
 * `import.meta.server` / `useRequestHeaders` itself — so the whole thing is
 * directly unit testable. `import.meta.server` is inlined to a static
 * `false` under this project's Vitest setup (plain `happy-dom`, not Nuxt's
 * `environment: 'nuxt'`) and cannot be toggled per test, and
 * `useRequestHeaders` silently returns `{}` outside a real request context —
 * neither can drive a meaningful test here. `useGraphql` below is the thin,
 * Nuxt-coupled adapter that supplies the real value.
 */
export async function runGraphql<TResult, TVars extends Record<string, unknown>>(
  document: TypedDocumentNode<TResult, TVars>,
  variables: TVars,
  headers?: Record<string, string>,
): Promise<TResult> {
  const query = printDocument(document)

  const response = await $fetch<GraphqlEnvelope<TResult>>('/api/graphql', {
    method: 'POST',
    headers,
    body: { query, variables },
  })

  if (response.errors?.length) {
    throw createError({ statusCode: 400, statusMessage: response.errors[0]?.message ?? 'GraphQL error' })
  }

  return response.data as TResult
}

/**
 * The one call site page code uses to run a GraphQL operation.
 *
 * Both contexts go through the same-origin proxy (`/api/graphql`), which
 * owns the one place the httpOnly cookie becomes a bearer header.
 *
 * In the browser this is a real request, and the browser already attaches
 * the httpOnly cookie itself — no header needs to be built here. During
 * SSR, Nitro handles `$fetch` to its own route in-process (no network hop,
 * no browser cookie jar involved), so the only extra work is forwarding the
 * incoming request's cookie header for the proxy to read.
 *
 * Routing SSR through the in-process proxy rather than calling the API
 * directly keeps the cookie-to-bearer translation in exactly one file, and
 * avoids importing `server/` code from `app/` — a boundary Nuxt does not
 * guarantee is safe to cross.
 */
export async function useGraphql<TResult, TVars extends Record<string, unknown>>(
  document: TypedDocumentNode<TResult, TVars>,
  variables: TVars,
): Promise<TResult> {
  const headers = import.meta.server ? useRequestHeaders(['cookie']) : undefined

  return runGraphql(document, variables, headers)
}
