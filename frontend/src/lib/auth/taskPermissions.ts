/**
 * Per-task permission rules, mirroring `Common/Security/TaskAccess.cs` and
 * `Features/Tasks/TaskStatusTransitions.cs` on the server.
 *
 * Unlike the member screens, `usePermissions()` from `/auth/me` is **not** enough here: task
 * rights depend on the task itself. A `DeptPresident` may edit a departmental task of their
 * own department but not of another one, and `OrgSecretary` sees every task while being
 * allowed to edit none.
 *
 * This is a UI concern only — the API enforces the same rules and answers 403 (or 404 for a
 * task the caller may not see) no matter what is rendered.
 */

import type { TaskListItemDto } from '@/lib/types/dto'
import type { DepartmentCode, SystemRole, TaskScope, TaskStatus } from '@/lib/types/enums'

/** The signed-in member, as far as these rules are concerned. */
export interface TaskActor {
  id: string
  role: SystemRole
  department: DepartmentCode | null
}

/** The task fields the rules read; satisfied by both the list and the detail DTO. */
type TaskLike = Pick<TaskListItemDto, 'scope' | 'department' | 'status'> & {
  assignees?: { id: string }[]
}

const ORG_LEADERSHIP: readonly SystemRole[] = ['OrgPresident', 'OrgVicePresident']
const DEPT_LEADERSHIP: readonly SystemRole[] = ['DeptPresident', 'DeptVicePresident']

function isOrgLeadership(actor: TaskActor): boolean {
  return ORG_LEADERSHIP.includes(actor.role)
}

function isDeptLeadership(actor: TaskActor): boolean {
  return DEPT_LEADERSHIP.includes(actor.role)
}

/** Mirrors `TaskAccess.CanEdit`. Also governs cancelling. */
export function canEditTask(task: TaskLike, actor: TaskActor): boolean {
  if (isOrgLeadership(actor)) return true

  if (isDeptLeadership(actor)) {
    return task.scope === 'Departmental' && task.department === actor.department
  }

  return false
}

/** Mirrors `TaskAccess.CanCancel`, which delegates to `CanEdit`. */
export function canCancelTask(task: TaskLike, actor: TaskActor): boolean {
  return canEditTask(task, actor)
}

/** `DELETE /tasks/{id}` is behind the `OrgPresidentOnly` policy. */
export function canDeleteTask(actor: TaskActor): boolean {
  return actor.role === 'OrgPresident'
}

/**
 * Mirrors `CreateTask.EnsureScopeAllowed`. The controller's `CanCreateDeptTask` policy is the
 * broad door; this is the precise rule.
 */
export function canCreateScope(scope: TaskScope, actor: TaskActor): boolean {
  if (scope === 'Organizational') return isOrgLeadership(actor)

  // Departmental: org leadership may target any department, dept leadership only their own.
  return isOrgLeadership(actor) || isDeptLeadership(actor)
}

/** True when the actor may create a task at all — used to show or hide the "new task" button. */
export function canCreateAnyTask(actor: TaskActor): boolean {
  return canCreateScope('Organizational', actor) || canCreateScope('Departmental', actor)
}

export function isAssignee(task: TaskLike, actor: TaskActor): boolean {
  return task.assignees?.some((assignee) => assignee.id === actor.id) ?? false
}

/** `PATCH /tasks/{id}/status` allows assignees and anyone who may edit the task. */
export function canChangeTaskStatus(task: TaskLike, actor: TaskActor): boolean {
  return isAssignee(task, actor) || canEditTask(task, actor)
}

/** The two self-service steps an assignee may take, from `TaskStatusTransitions`. */
const ASSIGNEE_MOVES: Partial<Record<TaskStatus, TaskStatus[]>> = {
  New: ['InProgress'],
  InProgress: ['InReview'],
}

/** Board columns, in order. `Cancelled` is deliberately absent. */
const BOARD_STATUSES: TaskStatus[] = ['New', 'InProgress', 'InReview', 'Completed']

/**
 * Statuses the actor may move this task to right now.
 *
 * `Cancelled` is never offered: cancelling goes through the idempotent
 * `POST /tasks/{id}/cancel` instead, which does not 409 when repeated.
 * The current status is excluded because the server rejects a no-op transition with 409.
 */
export function allowedTransitions(task: TaskLike, actor: TaskActor): TaskStatus[] {
  if (canEditTask(task, actor)) {
    return BOARD_STATUSES.filter((status) => status !== task.status)
  }

  if (isAssignee(task, actor)) {
    return ASSIGNEE_MOVES[task.status] ?? []
  }

  return []
}

/** Convenience for building an actor from the `/auth/me` response. */
export function toTaskActor(user: {
  id: string
  role: SystemRole
  department: DepartmentCode | null
}): TaskActor {
  return { id: user.id, role: user.role, department: user.department }
}
