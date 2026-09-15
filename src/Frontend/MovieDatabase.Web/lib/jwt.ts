const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

export interface JwtPayload {
  /** The API puts the user id in the jti claim. */
  jti: string
  sub: string
  email: string
  role: string | null
  /** Seconds since the Unix epoch. */
  exp: number
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const parts = token.split('.')
  if (parts.length !== 3) {
    return null
  }

  try {
    const base64 = parts[1]!.replace(/-/g, '+').replace(/_/g, '/')
    const raw = JSON.parse(atob(base64)) as Record<string, unknown>

    if (typeof raw.exp !== 'number') {
      return null
    }

    const role = raw.role ?? raw[ROLE_CLAIM_URI]

    return {
      jti: String(raw.jti ?? ''),
      sub: String(raw.sub ?? ''),
      email: String(raw.email ?? ''),
      role: typeof role === 'string' ? role : null,
      exp: raw.exp,
    }
  }
  catch {
    return null
  }
}

export function isExpired(payload: JwtPayload, skewSeconds = 0, now = Date.now()): boolean {
  return payload.exp * 1000 <= now + skewSeconds * 1000
}
