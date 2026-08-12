import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { getMe, login, logout, type LoginRequest } from '@/lib/api/auth'
import { setSessionExpiredHandler } from '@/lib/api/client'
import type { MeResponse } from '@/lib/types/dto'

import { AuthContext, type AuthContextValue, type AuthStatus } from './context'
import { requiresPasswordChange } from './jwt'
import { clearTokens, getTokens, onTokensChangedInAnotherTab, setTokens } from './tokenStorage'

/**
 * Owns the session: restores it on load, exposes sign-in/sign-out, and keeps the
 * password-change gate in sync with the claims inside the current access token.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<MeResponse | null>(null)
  const [mustChangePassword, setMustChangePassword] = useState(false)

  const clearSession = useCallback(() => {
    clearTokens()
    setUser(null)
    setMustChangePassword(false)
    setStatus('unauthenticated')
    // Cached responses belong to the member who just left.
    queryClient.clear()
  }, [queryClient])

  /**
   * Loads the profile for whatever tokens are currently stored.
   * `GET /auth/me` bypasses the password-change gate, so it also works for a gated member.
   */
  const loadSession = useCallback(async () => {
    const tokens = getTokens()
    if (!tokens) {
      setStatus('unauthenticated')
      return
    }

    setMustChangePassword(requiresPasswordChange(tokens.accessToken))

    try {
      setUser(await getMe())
      setStatus('authenticated')
    } catch {
      // The stored pair is expired, revoked, or belongs to a deactivated account.
      clearSession()
    }
  }, [clearSession])

  // Restore the session on first render.
  useEffect(() => {
    void loadSession()
  }, [loadSession])

  // The API client cannot reach React state, so it reports an unrecoverable 401 through here.
  useEffect(() => {
    setSessionExpiredHandler(clearSession)
    return () => setSessionExpiredHandler(null)
  }, [clearSession])

  // Follow sign-in / sign-out performed in another tab.
  useEffect(
    () =>
      onTokensChangedInAnotherTab((tokens) => {
        if (tokens) {
          void loadSession()
        } else {
          clearSession()
        }
      }),
    [clearSession, loadSession],
  )

  const signIn = useCallback(async (request: LoginRequest) => {
    const tokens = await login(request)
    setTokens({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken })
    setMustChangePassword(tokens.mustChangePassword)

    // A gated member may still read their own profile, so the shell can greet them by name.
    setUser(await getMe())
    setStatus('authenticated')
  }, [])

  const signOut = useCallback(async () => {
    const refreshToken = getTokens()?.refreshToken
    if (refreshToken) {
      try {
        await logout(refreshToken)
      } catch {
        // Revoking server-side is best-effort; the local session goes away either way.
      }
    }
    clearSession()
  }, [clearSession])

  const refreshUser = useCallback(async () => {
    setUser(await getMe())
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      mustChangePassword,
      signIn,
      signOut,
      endSession: clearSession,
      refreshUser,
    }),
    [status, user, mustChangePassword, signIn, signOut, clearSession, refreshUser],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
