import { useContext } from 'react'

import { AuthContext, NO_PERMISSIONS, type AuthContextValue } from './context'

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth трябва да се използва вътре в <AuthProvider>.')
  }
  return context
}

/**
 * The current member's permission flags, as computed by the server from their role.
 * Everything is false while signed out, so callers can read the flags unconditionally.
 */
export function usePermissions() {
  return useAuth().user?.permissions ?? NO_PERMISSIONS
}
