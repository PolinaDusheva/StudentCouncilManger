/**
 * Per-event permission rules, mirroring `Common/Security/EventAccess.cs`.
 *
 * Two things differ from tasks and are easy to get wrong:
 *
 * 1. **There is no visibility dimension.** Every authenticated member sees every event, so a
 *    404 from the events API is always a genuine "does not exist", never a hidden one.
 * 2. **Edit and delete are not the same set.** Editing is owner-based (the organiser may edit
 *    their own event); deleting is *not* — a secretary who created an event may edit it but
 *    must not delete it.
 */

import type { DepartmentCode, SystemRole } from '@/lib/types/enums'

export interface EventActor {
  id: string
  role: SystemRole
  department: DepartmentCode | null
}

/** The event fields the rules read; satisfied by both the list and the detail DTO. */
export interface EventLike {
  department: DepartmentCode | null
  organizer: { id: string } | null
  /** Virtual entries generated from a task deadline; never editable. */
  isDeadline: boolean
}

const ORG_LEADERSHIP: readonly SystemRole[] = ['OrgPresident', 'OrgVicePresident']
const DEPT_LEADERSHIP: readonly SystemRole[] = ['DeptPresident', 'DeptVicePresident']

function isOrgLeadership(actor: EventActor): boolean {
  return ORG_LEADERSHIP.includes(actor.role)
}

function isOwnDepartment(event: EventLike, actor: EventActor): boolean {
  return event.department !== null && event.department === actor.department
}

/**
 * `CanManageEvents` policy: every role except a plain `Member` may create events.
 * Used to show or hide the "new event" button.
 */
export function canCreateEvent(actor: EventActor): boolean {
  return actor.role !== 'Member'
}

/** Mirrors `EventAccess.CanEdit` — organiser, org leadership, or dept leadership for own dept. */
export function canEditEvent(event: EventLike, actor: EventActor): boolean {
  // A task deadline is a projection, not a real event: it is edited through the task.
  if (event.isDeadline) return false

  if (event.organizer?.id === actor.id || isOrgLeadership(actor)) return true

  return DEPT_LEADERSHIP.includes(actor.role) && isOwnDepartment(event, actor)
}

/**
 * Mirrors `EventAccess.CanDelete`. Deliberately **not** owner-based: org leadership, or dept
 * leadership for their own department. The organiser alone is not enough.
 */
export function canDeleteEvent(event: EventLike, actor: EventActor): boolean {
  if (event.isDeadline) return false

  if (isOrgLeadership(actor)) return true

  return DEPT_LEADERSHIP.includes(actor.role) && isOwnDepartment(event, actor)
}

/** Builds an actor from the `/auth/me` response. */
export function toEventActor(user: {
  id: string
  role: SystemRole
  department: DepartmentCode | null
}): EventActor {
  return { id: user.id, role: user.role, department: user.department }
}
