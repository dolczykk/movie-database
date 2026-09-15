export const DEFAULT_LOGIN_REDIRECT = '/films'

/** An origin that can never match a real site, used only to detect whether
 * parsing `redirect` as a URL escapes onto a different origin. */
const PROBE_ORIGIN = 'http://internal.invalid'

/**
 * Resolves the destination to send a user to after a successful login.
 *
 * `redirect` is `route.query.redirect`, which is attacker-controllable --
 * anyone can craft `/login?redirect=https://evil.com` and send it to a
 * victim, who authenticates for real and is then bounced to the attacker's
 * site. Route guards only ever write a same-origin relative `to.fullPath`
 * into this param (see `guardDecision`), so anything else falls back to
 * `/films`:
 *
 * - non-strings: missing (`undefined`), `null`, or a repeated query param
 *   arriving as an array
 * - empty or whitespace-only strings
 * - anything not starting with a single `/` -- absolute URLs
 *   (`https://evil.com`), and scheme URLs (`mailto:`, `javascript:`)
 * - a bare protocol-relative URL (`//evil.com`)
 * - inputs that *look* like a same-origin path but resolve off-origin once
 *   parsed as a URL, e.g. `/\evil.com` or `/\t/evil.com` -- the
 *   backslash-as-slash and control-character-stripping rules the URL spec
 *   applies turn these into a protocol-relative `//evil.com` in disguise.
 *   This is why the check below parses the value with `URL` and compares
 *   origins, rather than only checking string prefixes.
 */
export function resolveLoginRedirect(redirect: unknown): string {
  if (typeof redirect !== 'string' || redirect.trim().length === 0) {
    return DEFAULT_LOGIN_REDIRECT
  }

  if (!redirect.startsWith('/') || redirect.startsWith('//')) {
    return DEFAULT_LOGIN_REDIRECT
  }

  let parsed: URL
  try {
    parsed = new URL(redirect, PROBE_ORIGIN)
  }
  catch {
    return DEFAULT_LOGIN_REDIRECT
  }

  if (parsed.origin !== PROBE_ORIGIN) {
    return DEFAULT_LOGIN_REDIRECT
  }

  return redirect
}
