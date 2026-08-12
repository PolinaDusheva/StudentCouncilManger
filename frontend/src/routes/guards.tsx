import { Navigate, Outlet, useLocation } from 'react-router-dom'

import { Spinner } from '@/components/ui/Spinner'
import { useAuth, usePermissions } from '@/lib/auth/useAuth'
import type { PermissionSet } from '@/lib/types/dto'

export const CHANGE_PASSWORD_PATH = '/change-password'
export const LOGIN_PATH = '/login'
export const HOME_PATH = '/'

/**
 * Gate for the signed-in area. Also enforces the first-login password change: while the
 * flag is set the API answers 403 `password_change_required` to everything else, so there
 * is nothing useful to render until it is cleared.
 */
export function RequireAuth() {
  const { status, mustChangePassword } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return <Spinner className="min-h-dvh" label="Проверка на сесията…" />
  }

  if (status === 'unauthenticated') {
    // Remember where the member was headed so sign-in can return them there.
    return <Navigate to={LOGIN_PATH} state={{ from: location }} replace />
  }

  if (mustChangePassword && location.pathname !== CHANGE_PASSWORD_PATH) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace />
  }

  return <Outlet />
}

/**
 * Hides a route the member's role does not allow. This is a usability guard, not a security
 * boundary — the API enforces the same policy and answers 403 regardless of what is rendered.
 */
export function RequirePermission({ permission }: { permission: keyof PermissionSet }) {
  const permissions = usePermissions()

  return permissions[permission] ? <Outlet /> : <Navigate to={HOME_PATH} replace />
}

/** Keeps a signed-in member out of the login and password-recovery screens. */
export function RequireAnonymous() {
  const { status, mustChangePassword } = useAuth()

  if (status === 'loading') {
    return <Spinner className="min-h-dvh" label="Проверка на сесията…" />
  }

  if (status === 'authenticated') {
    return <Navigate to={mustChangePassword ? CHANGE_PASSWORD_PATH : HOME_PATH} replace />
  }

  return <Outlet />
}
