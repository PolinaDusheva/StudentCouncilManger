/**
 * Minimal read-only access to the claims inside the access token.
 *
 * The payload is decoded, never verified — the signature, the `stamp` claim and expiry are
 * all enforced by the API on every request. This exists purely so the UI can route correctly
 * on a page reload, when `GET /auth/me` alone cannot tell us whether the password-change gate
 * is active (that flag lives in the token, not in the `me` response).
 */

/** Claims issued by JwtTokenService. Absent optional claims mean "false"/"none". */
export interface AccessTokenClaims {
  sub?: string
  email?: string
  name?: string
  role?: string
  /** Present and `"true"` only while the first-login password change is pending. */
  must_change_password?: string
  dept?: string
  /** Expiry, seconds since the epoch. */
  exp?: number
}

export function decodeAccessToken(token: string): AccessTokenClaims | null {
  const payload = token.split('.')[1]
  if (!payload) return null

  try {
    // JWTs use base64url; restore the standard alphabet and the stripped padding.
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(payload.length / 4) * 4, '=')
    const json = new TextDecoder().decode(Uint8Array.from(atob(base64), (char) => char.charCodeAt(0)))
    return JSON.parse(json) as AccessTokenClaims
  } catch {
    // A malformed token is treated as "no claims"; the API will reject it anyway.
    return null
  }
}

/** True while the member still has to replace their temporary password. */
export function requiresPasswordChange(token: string): boolean {
  return decodeAccessToken(token)?.must_change_password === 'true'
}
