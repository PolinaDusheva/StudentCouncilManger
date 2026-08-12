import { createContext } from 'react'

import type { LoginRequest } from '@/lib/api/auth'
import type { MeResponse, PermissionSet } from '@/lib/types/dto'

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

export interface AuthContextValue {
  status: AuthStatus
  /** Profile, role and permissions; null until the session is established. */
  user: MeResponse | null
  /**
   * True while the member still has a temporary password. The API answers 403
   * `password_change_required` to every endpoint except auth until it is replaced.
   */
  mustChangePassword: boolean
  signIn: (request: LoginRequest) => Promise<void>
  signOut: () => Promise<void>
  /**
   * Drops the local session without calling the API. Used after a password change, which
   * server-side rotates the security stamp and revokes every refresh token — the current
   * tokens are already dead, so signing out "properly" would only produce a 401.
   */
  endSession: () => void
  /** Re-reads `GET /auth/me`, e.g. after the member edits their own profile. */
  refreshUser: () => Promise<void>
}

/** Convenience view of the permission flags with everything false (signed-out default). */
export const NO_PERMISSIONS: PermissionSet = {
  canManageMembers: false,
  canManageBudget: false,
  canManageDuties: false,
  canCreateOrgTask: false,
  canCreateDeptTask: false,
  canManageEvents: false,
}

export const AuthContext = createContext<AuthContextValue | null>(null)
