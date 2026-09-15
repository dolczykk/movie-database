import { clearAuthCookies } from '../../utils/authCookies'

export default defineEventHandler((event) => {
  clearAuthCookies(event)
  return { ok: true }
})
