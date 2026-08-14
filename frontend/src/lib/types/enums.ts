/**
 * Enum values from `.ai/api-requests.xsd` section 2 (ИЗБРОИМИ ТИПОВЕ).
 *
 * The API serialises enums as strings (JsonStringEnumConverter), case-sensitive in
 * request bodies and case-insensitive in query filters. These objects are the single
 * source of truth on the client — they feed both the TS types and the Zod schemas,
 * so an enum added to the XSD only has to be added here.
 */

export const SYSTEM_ROLES = [
  'Member',
  'DeptSecretary',
  'DeptVicePresident',
  'DeptPresident',
  'OrgSecretary',
  'OrgVicePresident',
  'OrgPresident',
] as const
export type SystemRole = (typeof SYSTEM_ROLES)[number]

/**
 * Organisation-level roles. Mirrors `RoleRules.IsOrgRole` on the server, which enforces the
 * matching rule: an org role must NOT have a department, every other role must have one.
 */
export const ORG_ROLES: readonly SystemRole[] = ['OrgSecretary', 'OrgVicePresident', 'OrgPresident']

export function isOrgRole(role: SystemRole): boolean {
  return ORG_ROLES.includes(role)
}

export const DEPARTMENT_CODES = ['PR', 'Projects', 'Social', 'Sports'] as const
export type DepartmentCode = (typeof DEPARTMENT_CODES)[number]

export const MEMBER_STATUSES = ['Active', 'Inactive'] as const
export type MemberStatus = (typeof MEMBER_STATUSES)[number]

export const TASK_SCOPES = ['Organizational', 'Departmental'] as const
export type TaskScope = (typeof TASK_SCOPES)[number]

export const TASK_PRIORITIES = ['Low', 'Medium', 'High', 'Critical'] as const
export type TaskPriority = (typeof TASK_PRIORITIES)[number]

export const TASK_STATUSES = ['New', 'InProgress', 'InReview', 'Completed', 'Cancelled'] as const
export type TaskStatus = (typeof TASK_STATUSES)[number]

export const EVENT_TYPES = [
  'Meeting',
  'PublicEvent',
  'InternalMeeting',
  'SportsEvent',
  'Deadline',
  'Other',
] as const
export type EventType = (typeof EVENT_TYPES)[number]

export const RECURRENCE_TYPES = ['None', 'Weekly', 'Monthly'] as const
export type RecurrenceType = (typeof RECURRENCE_TYPES)[number]

export const DEVICE_PLATFORMS = ['Android', 'iOS'] as const
export type DevicePlatform = (typeof DEVICE_PLATFORMS)[number]

export const CALENDAR_VIEWS = ['day', 'week', 'month', 'list'] as const
export type CalendarView = (typeof CALENDAR_VIEWS)[number]

/**
 * Sort values accepted by `GET /tasks` (XSD `TaskSort`). A `-` prefix reverses the direction;
 * anything unrecognised falls back to `createdAt` descending on the server.
 */
export const TASK_SORTS = [
  'dueAt',
  '-dueAt',
  'priority',
  '-priority',
  'status',
  '-status',
  'title',
  '-title',
  'createdAt',
  '-createdAt',
] as const
export type TaskSort = (typeof TASK_SORTS)[number]

/**
 * Sort values accepted by `GET /members` (XSD `MemberSort`). A `-` prefix reverses the
 * direction; anything unrecognised falls back to `fullName` ascending on the server.
 */
export const MEMBER_SORTS = [
  'fullName',
  '-fullName',
  'joinedOn',
  '-joinedOn',
  'role',
  '-role',
] as const
export type MemberSort = (typeof MEMBER_SORTS)[number]

/** MIME types accepted for a profile photo (AllowedFileTypes.Images). */
export const IMAGE_CONTENT_TYPES = ['image/jpeg', 'image/png', 'image/heic', 'image/heif'] as const
export type ImageContentType = (typeof IMAGE_CONTENT_TYPES)[number]

/**
 * MIME types accepted for a task document (AllowedFileTypes.Documents).
 * The server additionally checks the extension and the file's magic bytes.
 */
export const DOCUMENT_CONTENT_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/msword',
  'text/plain',
  'application/vnd.oasis.opendocument.text',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/vnd.ms-excel',
  'text/csv',
  'application/csv',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  'application/vnd.ms-powerpoint',
  'image/jpeg',
  'image/png',
  'image/gif',
  'image/heic',
  'image/heif',
] as const
export type DocumentContentType = (typeof DOCUMENT_CONTENT_TYPES)[number]

/** Bulgarian labels for the role enum, used across the UI. */
export const SYSTEM_ROLE_LABELS: Record<SystemRole, string> = {
  Member: 'Член',
  DeptSecretary: 'Секретар на отдел',
  DeptVicePresident: 'Зам.-председател на отдел',
  DeptPresident: 'Председател на отдел',
  OrgSecretary: 'Секретар на организацията',
  OrgVicePresident: 'Зам.-председател на организацията',
  OrgPresident: 'Председател на организацията',
}

export const DEPARTMENT_CODE_LABELS: Record<DepartmentCode, string> = {
  PR: 'ПР',
  Projects: 'Проекти',
  Social: 'Социални дейности',
  Sports: 'Спорт',
}

export const MEMBER_STATUS_LABELS: Record<MemberStatus, string> = {
  Active: 'Активен',
  Inactive: 'Неактивен',
}
