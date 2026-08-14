/**
 * `/api/v1/tasks` — request schemas and calls.
 *
 * Mirrors section 7 of `.ai/api-requests.xsd`. The scope ↔ department rule additionally
 * mirrors `CreateTaskValidator` on the server, which the XSD cannot express.
 */

import { z } from 'zod'

import type {
  PagedResult,
  TaskBoardDto,
  TaskCommentDto,
  TaskDetailDto,
  TaskListItemDto,
} from '@/lib/types/dto'
import {
  TASK_PRIORITIES,
  TASK_SCOPES,
  type DepartmentCode,
  type TaskPriority,
  type TaskScope,
  type TaskSort,
  type TaskStatus,
} from '@/lib/types/enums'
import { guidSchema } from '@/lib/validation/common'

import { apiFetch, type QueryParams } from './client'

// ---------------------------------------------------------------- filters

/** Query for `GET /tasks`; every field is optional and empty values are dropped. */
export interface TaskFilters {
  scope?: TaskScope | ''
  department?: DepartmentCode | ''
  /** Member id — only tasks with this assignee. */
  assignee?: string
  priority?: TaskPriority | ''
  status?: TaskStatus | ''
  /** ISO-8601 UTC bounds on the due date. */
  from?: string
  to?: string
  overdue?: boolean
  sort?: TaskSort | ''
  page?: number
  pageSize?: number
}

// ---------------------------------------------------------------- schemas

const taskFieldsSchema = z.object({
  title: z
    .string()
    .trim()
    .min(3, 'Заглавието трябва да е поне 3 символа.')
    .max(150, 'Заглавието не може да е по-дълго от 150 символа.'),
  description: z.string().trim().max(4000, 'Описанието не може да е по-дълго от 4000 символа.'),
  priority: z.enum(TASK_PRIORITIES),
  /** Empty string means "no due date" — `<input type="datetime-local">` cannot hold null. */
  dueAtLocal: z.string(),
  assigneeIds: z.array(guidSchema).min(1, 'Избери поне един изпълнител.'),
})

/**
 * The due date is entered in the browser's local zone but sent as UTC. Checked here so an
 * obviously past date fails before the request; the server re-checks against its own clock.
 */
function checkDueInFuture(values: { dueAtLocal: string }, context: z.RefinementCtx) {
  if (values.dueAtLocal === '') return

  const due = new Date(values.dueAtLocal)
  if (Number.isNaN(due.getTime())) {
    context.addIssue({ code: 'custom', path: ['dueAtLocal'], message: 'Невалидна дата.' })
    return
  }

  if (due.getTime() <= Date.now()) {
    context.addIssue({
      code: 'custom',
      path: ['dueAtLocal'],
      message: 'Крайният срок трябва да е в бъдещето.',
    })
  }
}

/**
 * Organisational tasks must not carry a department; departmental ones must.
 * Same shape of rule as role ↔ department for members.
 */
function checkScopeDepartmentMatch(
  values: { scope: TaskScope; departmentId: string },
  context: z.RefinementCtx,
) {
  if (values.scope === 'Organizational' && values.departmentId !== '') {
    context.addIssue({
      code: 'custom',
      path: ['departmentId'],
      message: 'Организационните задачи не се числят към отдел.',
    })
  }

  if (values.scope === 'Departmental' && values.departmentId === '') {
    context.addIssue({
      code: 'custom',
      path: ['departmentId'],
      message: 'Департаментната задача изисква отдел.',
    })
  }
}

/** XSD `CreateTaskRequest`. */
export const createTaskSchema = taskFieldsSchema
  .extend({
    scope: z.enum(TASK_SCOPES),
    departmentId: z.union([guidSchema, z.literal('')]),
  })
  .superRefine((values, context) => {
    checkScopeDepartmentMatch(values, context)
    checkDueInFuture(values, context)
  })
export type CreateTaskForm = z.infer<typeof createTaskSchema>

/**
 * XSD `UpdateTaskRequest`. Scope and department are **not** editable after creation, but the
 * fields stay in the shape so one form component can serve both create and edit.
 */
export const updateTaskSchema = taskFieldsSchema
  .extend({
    scope: z.enum(TASK_SCOPES),
    departmentId: z.union([guidSchema, z.literal('')]),
  })
  .superRefine(checkDueInFuture)
export type UpdateTaskForm = z.infer<typeof updateTaskSchema>

/** XSD `AddTaskCommentRequest`. */
export const addCommentSchema = z.object({
  text: z
    .string()
    .trim()
    .min(1, 'Коментарът не може да е празен.')
    .max(2000, 'Коментарът не може да е по-дълъг от 2000 символа.'),
})
export type AddCommentForm = z.infer<typeof addCommentSchema>

/**
 * `datetime-local` gives wall-clock text with no zone; the API wants UTC.
 * An empty value means "no due date" and is sent as null.
 */
export function toUtcOrNull(localValue: string): string | null {
  return localValue === '' ? null : new Date(localValue).toISOString()
}

/** Inverse of {@link toUtcOrNull}, for populating the edit form. */
export function toLocalInputValue(utc: string | null): string {
  if (!utc) return ''

  const date = new Date(utc)
  if (Number.isNaN(date.getTime())) return ''

  // `datetime-local` needs `YYYY-MM-DDTHH:mm` in local time, so the zone offset is removed.
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

// ---------------------------------------------------------------- calls

/** `GET /tasks` — paginated and filterable, limited to what the caller may see. */
export function getTasks(filters: TaskFilters): Promise<PagedResult<TaskListItemDto>> {
  return apiFetch<PagedResult<TaskListItemDto>>('/tasks', { query: filters as QueryParams })
}

/**
 * `GET /tasks/mine` — tasks assigned to the caller.
 *
 * Returns a **plain array**, not a `PagedResult`, and accepts no parameters: filters,
 * sorting and paging do not apply (`GetMyTasksQuery` is an empty record).
 */
export function getMyTasks(): Promise<TaskListItemDto[]> {
  return apiFetch<TaskListItemDto[]>('/tasks/mine')
}

/** `GET /tasks/board` — Kanban columns. Takes no parameters: filters do not apply. */
export function getTaskBoard(): Promise<TaskBoardDto> {
  return apiFetch<TaskBoardDto>('/tasks/board')
}

/** `GET /tasks/{id}` — 404 (not 403) when the caller may not see it. */
export function getTask(id: string): Promise<TaskDetailDto> {
  return apiFetch<TaskDetailDto>(`/tasks/${id}`)
}

export function createTask(values: CreateTaskForm): Promise<TaskDetailDto> {
  return apiFetch<TaskDetailDto>('/tasks', {
    method: 'POST',
    body: {
      title: values.title,
      description: values.description === '' ? null : values.description,
      priority: values.priority,
      scope: values.scope,
      departmentId: values.departmentId === '' ? null : values.departmentId,
      dueAtUtc: toUtcOrNull(values.dueAtLocal),
      assigneeIds: values.assigneeIds,
    },
  })
}

/** `PUT /tasks/{id}` — scope and department are fixed at creation and not sent. */
export function updateTask(id: string, values: UpdateTaskForm): Promise<TaskDetailDto> {
  return apiFetch<TaskDetailDto>(`/tasks/${id}`, {
    method: 'PUT',
    body: {
      title: values.title,
      description: values.description === '' ? null : values.description,
      priority: values.priority,
      dueAtUtc: toUtcOrNull(values.dueAtLocal),
      assigneeIds: values.assigneeIds,
    },
  })
}

/** `PATCH /tasks/{id}/status` — a no-op transition is rejected with 409. */
export function changeTaskStatus(id: string, status: TaskStatus): Promise<TaskDetailDto> {
  return apiFetch<TaskDetailDto>(`/tasks/${id}/status`, { method: 'PATCH', body: { status } })
}

/** `POST /tasks/{id}/cancel` — idempotent, unlike setting the status to `Cancelled`. */
export function cancelTask(id: string): Promise<void> {
  return apiFetch<void>(`/tasks/${id}/cancel`, { method: 'POST' })
}

/** `DELETE /tasks/{id}` — `OrgPresident` only. */
export function deleteTask(id: string): Promise<void> {
  return apiFetch<void>(`/tasks/${id}`, { method: 'DELETE' })
}

/** `GET /tasks/{id}/comments`. */
export function getTaskComments(id: string): Promise<TaskCommentDto[]> {
  return apiFetch<TaskCommentDto[]>(`/tasks/${id}/comments`)
}

/** `POST /tasks/{id}/comments` — anyone who can see the task may comment. */
export function addTaskComment(id: string, values: AddCommentForm): Promise<TaskCommentDto> {
  return apiFetch<TaskCommentDto>(`/tasks/${id}/comments`, { method: 'POST', body: values })
}
