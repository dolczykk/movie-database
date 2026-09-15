import { createError } from 'h3'
import { decodeJwtPayload } from '../../../lib/jwt'
import { clearAuthCookies, readAuthCookies, setAuthCookies, type TokenPair } from '../../utils/authCookies'
import { callGraphql } from '../../utils/graphqlClient'
import { REVOKE_TOKEN_MUTATION } from '../../utils/authOperations'
import { singleFlight } from '../../utils/singleFlight'
import type { AuthUser } from '../../utils/authOperations'

interface RevokeResult {
  revoke: {
    accessToken: { value: string, expiresAt: string }
    refreshToken: { value: string, expiresAt: string }
  }
}

/**
 * A definitive rejection means the API was reached and refused the token
 * pair outright (`callGraphql` surfaces GraphQL-level errors as an h3 error
 * with `statusCode: 400`). Anything else — a network error, a 5xx, or
 * `callGraphql`'s own 502 for an empty response — is indeterminate: we
 * never learned whether the pair is actually still valid.
 */
export function isDefinitiveRejection(error: unknown): boolean {
  return typeof error === 'object' && error !== null
    && 'statusCode' in error && (error as { statusCode?: unknown }).statusCode === 400
}

export default defineEventHandler(async (event): Promise<AuthUser> => {
  const { accessToken, refreshToken } = readAuthCookies(event)

  if (!accessToken || !refreshToken) {
    throw createError({ statusCode: 401, statusMessage: 'Not authenticated' })
  }

  let pair: TokenPair
  try {
    // Namespaced so this key can never collide with another single-flight
    // caller sharing the same global keyspace (`server/utils/singleFlight.ts`
    // has no notion of its own of what a key "belongs to").
    pair = await singleFlight(`refresh:${refreshToken}`, async () => {
      const data = await callGraphql<RevokeResult>(
        event,
        REVOKE_TOKEN_MUTATION,
        { input: { accessToken, refreshToken } },
      )

      return {
        accessToken: data.revoke.accessToken.value,
        accessTokenExpiresAt: data.revoke.accessToken.expiresAt,
        refreshToken: data.revoke.refreshToken.value,
        refreshTokenExpiresAt: data.revoke.refreshToken.expiresAt,
      }
    })
  }
  catch (error) {
    if (isDefinitiveRejection(error)) {
      // The API was reached and rejected the pair (expired access token,
      // already-revoked pair, etc.) — the session really is over. Logs the
      // message only: the raw error's body carried both tokens.
      console.error('[auth/refresh] API rejected the token pair', error instanceof Error ? error.message : String(error))
      clearAuthCookies(event)
      throw createError({ statusCode: 401, statusMessage: 'Session expired' })
    }

    // A network error, a 5xx, or a bug — we could not learn whether the
    // pair is still valid, so don't log the user out on a guess. Task 11
    // calls this route on a timer for every logged-in user; treating an
    // indeterminate failure as a rejection would silently end otherwise
    // healthy sessions. Let the caller retry. Logs the message only -- see
    // the comment above.
    console.error('[auth/refresh] refresh failed indeterminately', error instanceof Error ? error.message : String(error))
    throw createError({ statusCode: 503, statusMessage: 'Unable to refresh session' })
  }

  setAuthCookies(event, pair)

  const payload = decodeJwtPayload(pair.accessToken)
  if (!payload) {
    throw createError({ statusCode: 502, statusMessage: 'Unreadable token from API' })
  }

  return {
    id: payload.jti,
    username: payload.sub,
    email: payload.email,
    role: payload.role,
  }
})
