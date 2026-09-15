import { describe, expect, it } from 'vitest'
import { resolveLoginRedirect } from '../../lib/loginRedirect'

describe('resolveLoginRedirect', () => {
  it('allows a same-origin relative path (the happy path)', () => {
    expect(resolveLoginRedirect('/films/42/edit')).toBe('/films/42/edit')
  })

  it('preserves a query string on the relative path', () => {
    expect(resolveLoginRedirect('/uploads/new?tab=cast')).toBe('/uploads/new?tab=cast')
  })

  it('falls back to /films when the param is missing (undefined)', () => {
    expect(resolveLoginRedirect(undefined)).toBe('/films')
  })

  it('falls back to /films for non-string values', () => {
    expect(resolveLoginRedirect(42)).toBe('/films')
    expect(resolveLoginRedirect(null)).toBe('/films')
    expect(resolveLoginRedirect({})).toBe('/films')
    expect(resolveLoginRedirect(true)).toBe('/films')
  })

  it('falls back to /films for a repeated query param arriving as an array', () => {
    // vue-router represents a repeated `?redirect=a&redirect=b` query param
    // as a string array rather than a single string.
    expect(resolveLoginRedirect(['/films', '/uploads'])).toBe('/films')
  })

  it('falls back to /films for an empty string', () => {
    expect(resolveLoginRedirect('')).toBe('/films')
  })

  it('falls back to /films for a whitespace-only string', () => {
    expect(resolveLoginRedirect('   ')).toBe('/films')
  })

  it('rejects an absolute http URL', () => {
    expect(resolveLoginRedirect('http://evil.com')).toBe('/films')
  })

  it('rejects an absolute https URL', () => {
    expect(resolveLoginRedirect('https://evil.com/phish')).toBe('/films')
  })

  it('rejects a bare protocol-relative URL', () => {
    expect(resolveLoginRedirect('//evil.com')).toBe('/films')
  })

  it('rejects a protocol-relative URL with a path', () => {
    expect(resolveLoginRedirect('//evil.com/films')).toBe('/films')
  })

  it('rejects non-http URL schemes', () => {
    expect(resolveLoginRedirect('mailto:x@evil.com')).toBe('/films')
    expect(resolveLoginRedirect('javascript:alert(1)')).toBe('/films')
  })

  it('rejects a backslash trick that the URL spec normalizes into a protocol-relative URL', () => {
    // WHATWG URL parsing treats "\" as "/" for special schemes, so a leading
    // "/\" is equivalent to "//" once parsed -- "/\evil.com" resolves to the
    // origin "http://evil.com", not the app's own origin.
    expect(resolveLoginRedirect('/\\evil.com')).toBe('/films')
  })

  it('rejects an embedded control character that the URL spec strips into a protocol-relative URL', () => {
    // The URL spec strips raw tab/newline/CR characters before parsing, so
    // the literal string "/<TAB>/evil.com" is parsed as "//evil.com".
    expect(resolveLoginRedirect('/\t/evil.com')).toBe('/films')
  })

  it('does not require the string to literally start with "//" to be rejected as off-origin', () => {
    // Guards against a naive `!value.startsWith('//')` implementation that
    // the two tricks above would slip past.
    expect(resolveLoginRedirect('/\\evil.com')).not.toBe('/\\evil.com')
    expect(resolveLoginRedirect('/\t/evil.com')).not.toBe('/\t/evil.com')
  })
})
