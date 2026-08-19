/**
 * Response shapes returned by the API.
 *
 * The XSD in `.ai/api-requests.xsd` only describes *requests*, so these mirror the
 * C# records in `backend/src/StudentCouncil.Application/Features/**` — serialised
 * camelCase by System.Text.Json. `DateTime` fields arrive as ISO-8601 UTC strings and
 * `DateOnly` fields as `YYYY-MM-DD`, so both are typed as `string` here.
 */

import type {
  DepartmentCode,
  EventType,
  MemberStatus,
  RecurrenceType,
  SystemRole,
  TaskPriority,
  TaskScope,
  TaskStatus,
} from './enums'

/** Standard pagination envelope returned by every list endpoint (PagedResult<T>). */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// ---------------------------------------------------------------- members

/** Compact member view used in lists, summaries and the auth `user` payload. */
export interface MemberSummaryDto {
  id: string
  fullName: string
  role: SystemRole
  department: DepartmentCode | null
  status: MemberStatus
  /** Relative API path (`/api/v1/members/{id}/photo`), or null when unset. */
  photoUrl: string | null
}

export interface MemberDto {
  id: string
  fullName: string
  email: string
  phoneNumber: string | null
  photoUrl: string | null
  role: SystemRole
  department: DepartmentCode | null
  departmentName: string | null
  /** DateOnly — `YYYY-MM-DD`. */
  joinedOn: string
  status: MemberStatus
}

// ---------------------------------------------------------------- auth

/** UI-facing permission flags derived from the role (mirrors the server auth policies). */
export interface PermissionSet {
  canManageMembers: boolean
  canManageBudget: boolean
  canManageDuties: boolean
  canCreateOrgTask: boolean
  canCreateDeptTask: boolean
  canManageEvents: boolean
}

/** `POST /auth/login` and `POST /auth/refresh`. */
export interface AuthTokensResponse {
  accessToken: string
  refreshToken: string
  expiresInSeconds: number
  mustChangePassword: boolean
  user: MemberSummaryDto
}

/** `GET /auth/me`. */
export interface MeResponse {
  id: string
  fullName: string
  email: string
  phoneNumber: string | null
  role: SystemRole
  department: DepartmentCode | null
  departmentName: string | null
  status: MemberStatus
  permissions: PermissionSet
}

// ---------------------------------------------------------------- departments

export interface DepartmentLeadershipDto {
  president: MemberSummaryDto | null
  vicePresident: MemberSummaryDto | null
  secretary: MemberSummaryDto | null
}

export interface DepartmentDto {
  id: string
  code: DepartmentCode
  name: string
  description: string | null
  memberCount: number
  leadership: DepartmentLeadershipDto
}

export interface DepartmentDetailDto extends DepartmentDto {
  members: MemberSummaryDto[]
}

// ---------------------------------------------------------------- tasks

export interface TaskListItemDto {
  id: string
  title: string
  priority: TaskPriority
  status: TaskStatus
  dueAtUtc: string | null
  scope: TaskScope
  department: DepartmentCode | null
  assigneeCount: number
  commentCount: number
  documentCount: number
  isOverdue: boolean
}

export interface TaskCommentDto {
  id: string
  author: MemberSummaryDto | null
  text: string
  createdAtUtc: string
}

export interface TaskDocumentDto {
  id: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  uploadedBy: MemberSummaryDto | null
  uploadedAtUtc: string
}

export interface TaskDetailDto {
  id: string
  title: string
  description: string | null
  priority: TaskPriority
  status: TaskStatus
  dueAtUtc: string | null
  scope: TaskScope
  department: DepartmentCode | null
  createdBy: MemberSummaryDto | null
  createdAtUtc: string
  assignees: MemberSummaryDto[]
  documents: TaskDocumentDto[]
  comments: TaskCommentDto[]
}

/** Kanban columns — `Cancelled` is intentionally not a column. */
export interface TaskBoardDto {
  columns: {
    new: TaskListItemDto[]
    inProgress: TaskListItemDto[]
    inReview: TaskListItemDto[]
    completed: TaskListItemDto[]
  }
}

// ---------------------------------------------------------------- events

export interface EventDto {
  id: string
  title: string
  description: string | null
  startUtc: string
  endUtc: string
  location: string | null
  type: EventType
  department: DepartmentCode | null
  organizer: MemberSummaryDto | null
  recurrence: RecurrenceType
  /** True for virtual entries generated from a task deadline. */
  isDeadline: boolean
  taskId: string | null
  /** Set on expanded occurrences of a recurring event. */
  occurrenceStartUtc: string | null
}

export interface EventDetailDto extends EventDto {
  participants: MemberSummaryDto[]
}

/** A schedule overlap surfaced as a non-blocking warning. */
export interface EventConflictDto {
  id: string
  title: string
  startUtc: string
  endUtc: string
}

export interface EventMutationResult {
  event: EventDetailDto
  conflictsWith: EventConflictDto[]
}

// ---------------------------------------------------------------- duties

export interface DutyRecordDto {
  id: string
  member: MemberSummaryDto | null
  startUtc: string
  endUtc: string
  durationMinutes: number
  periodYear: number
  periodMonth: number
  recordedBy: MemberSummaryDto | null
  note: string | null
}

export interface DutySummaryDto {
  member: MemberSummaryDto
  totalMinutes: number
  requiredMinutes: number
  metNorm: boolean
}

export interface MyDutySummaryDto {
  year: number
  month: number
  totalMinutes: number
  requiredMinutes: number
  metNorm: boolean
}

export interface RemindResult {
  notified: number
}

// ---------------------------------------------------------------- budget

export interface ExpenseDto {
  id: string
  description: string
  amountEur: number
  /** DateOnly — `YYYY-MM-DD`. */
  spentOn: string
  addedBy: MemberSummaryDto | null
  createdAtUtc: string
}

export interface BudgetSummaryDto {
  year: number
  totalEur: number
}

// ---------------------------------------------------------------- notifications

/**
 * Deep-link payload: `id` is present for `Task`/`Event`, absent for `Duty` (a duty reminder
 * links to the caller's own summary screen, not a single entity).
 */
export interface NotificationPayload {
  type: 'Task' | 'Event' | 'Duty'
  id: string | null
}

export interface NotificationDto {
  id: string
  /** Mirrors `NotificationType` in the domain (`TaskAssigned`, `EventReminder`, ...). */
  type: string
  title: string
  body: string
  payload: NotificationPayload | null
  isRead: boolean
  createdAtUtc: string
}
