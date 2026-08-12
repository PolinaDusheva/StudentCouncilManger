/**
 * Persistence for the access + refresh token pair.
 *
 * The backend rotates the refresh token on every `POST /auth/refresh`, so the stored
 * pair must always be written back together. `localStorage` keeps the session across
 * tab reloads; the `storage` event lets other tabs react to a login or logout.
 */

const ACCESS_TOKEN_KEY = 'sc.accessToken'
const REFRESH_TOKEN_KEY = 'sc.refreshToken'

export interface TokenPair {
  accessToken: string
  refreshToken: string
}

/** In-memory mirror so the request path never pays for a localStorage read. */
let cached: TokenPair | null = null
let loaded = false

export function getTokens(): TokenPair | null {
  if (!loaded) {
    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY)
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
    cached = accessToken && refreshToken ? { accessToken, refreshToken } : null
    loaded = true
  }
  return cached
}

export function setTokens(tokens: TokenPair): void {
  cached = tokens
  loaded = true
  localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken)
}

export function clearTokens(): void {
  cached = null
  loaded = true
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
}

/**
 * Notifies the caller when another tab writes or clears the token pair, so the app can
 * follow that tab into a signed-in or signed-out state. Returns an unsubscribe function.
 */
export function onTokensChangedInAnotherTab(listener: (tokens: TokenPair | null) => void): () => void {
  const handler = (event: StorageEvent) => {
    if (event.key !== ACCESS_TOKEN_KEY && event.key !== REFRESH_TOKEN_KEY) return

    // The cached copy belongs to this tab; force a re-read of what the other tab wrote.
    loaded = false
    listener(getTokens())
  }

  window.addEventListener('storage', handler)
  return () => window.removeEventListener('storage', handler)
}
