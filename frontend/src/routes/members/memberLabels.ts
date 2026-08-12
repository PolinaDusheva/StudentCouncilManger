/**
 * Screen-level presentation choices for member enums. Kept out of `Badge` so the component
 * stays a dumb container, and out of `enums.ts` so colour choices are not treated as data.
 */

import type { BadgeTone } from '@/components/ui/Badge'
import { isOrgRole, type MemberStatus, type SystemRole } from '@/lib/types/enums'

export function roleTone(role: SystemRole): BadgeTone {
  if (role === 'OrgPresident') return 'info'
  if (isOrgRole(role)) return 'neutral'
  if (role === 'Member') return 'neutral'
  // Department leadership.
  return 'warning'
}

export function statusTone(status: MemberStatus): BadgeTone {
  return status === 'Active' ? 'success' : 'neutral'
}
