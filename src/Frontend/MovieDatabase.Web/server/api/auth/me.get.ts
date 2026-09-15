import { decodeJwtPayload, isExpired } from '../../../lib/jwt'
import { readAuthCookies } from '../../utils/authCookies'
import type { AuthUser } from '../../utils/authOperations'

export default defineEventHandler((event): AuthUser | null => {
  // This response varies per visitor's cookie (id/username/email/role for
  // whoever's access token it is) and GETs without explicit cache
  // directives are heuristically cacheable by shared caches -- without
  // this, a proxy or CDN sitting in front could serve one user's identity
  // to another.
  setResponseHeader(event, 'cache-control', 'no-store')

  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    return null
  }

  const payload = decodeJwtPayload(accessToken)
  if (!payload || isExpired(payload)) {
    return null
  }

  return {
    id: payload.jti,
    username: payload.sub,
    email: payload.email,
    role: payload.role,
  }
})
