import { decodeJwtPayload, isExpired } from '../../../lib/jwt'
import { readAuthCookies } from '../../utils/authCookies'
import type { AuthUser } from '../../utils/authOperations'

export default defineEventHandler((event): { user: AuthUser | null } => {
  // This response varies per visitor's cookie (id/username/email/role for
  // whoever's access token it is) and GETs without explicit cache
  // directives are heuristically cacheable by shared caches -- without
  // this, a proxy or CDN sitting in front could serve one user's identity
  // to another.
  setResponseHeader(event, 'cache-control', 'no-store')

  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    // A bare `null` return makes h3 send an empty-bodied 204 (see
    // `handleHandlerResponse` in h3), which `useFetch` then parses as
    // `undefined` rather than `null` -- tripping Nuxt's NUXT_E3006
    // diagnostic. Wrapping in an object keeps the top-level return value
    // always JSON-serialisable.
    return { user: null }
  }

  const payload = decodeJwtPayload(accessToken)
  if (!payload || isExpired(payload)) {
    return { user: null }
  }

  return {
    user: {
      id: payload.jti,
      username: payload.sub,
      email: payload.email,
      role: payload.role,
    },
  }
})
