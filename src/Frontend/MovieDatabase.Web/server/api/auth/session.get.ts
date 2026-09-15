import { decodeJwtPayload, isExpired } from '../../../lib/jwt'
import { readAuthCookies } from '../../utils/authCookies'

export default defineEventHandler((event): { exp: number } | null => {
  // See me.get.ts -- this also varies per visitor's cookie.
  setResponseHeader(event, 'cache-control', 'no-store')

  const { accessToken } = readAuthCookies(event)
  if (!accessToken) {
    return null
  }

  const payload = decodeJwtPayload(accessToken)
  if (!payload || isExpired(payload)) {
    return null
  }

  return { exp: payload.exp }
})
