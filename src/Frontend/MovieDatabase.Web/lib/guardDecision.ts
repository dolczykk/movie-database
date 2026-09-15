export type GuardOutcome =
  | { type: 'allow' }
  | { type: 'redirect', to: { path: string, query: { redirect: string } } }
  | { type: 'forbid' }

/**
 * Decides what a route guard should do for a visitor.
 *
 * - Not authenticated -> `redirect` to `/login`, carrying `path` in the
 *   `redirect` query param so the caller can send them back after signing in.
 * - Authenticated but missing the capability the route requires -> `forbid`
 *   (a 403), not a redirect: sending them to a login page they are already
 *   past is a dead end.
 * - Authenticated and holding the capability -> `allow`.
 *
 * Authentication is checked before the capability, so an unauthenticated
 * visitor is always redirected, regardless of `hasCapability`.
 */
export function guardDecision(isAuthenticated: boolean, hasCapability: boolean, path: string): GuardOutcome {
  if (!isAuthenticated) {
    return { type: 'redirect', to: { path: '/login', query: { redirect: path } } }
  }

  if (!hasCapability) {
    return { type: 'forbid' }
  }

  return { type: 'allow' }
}
