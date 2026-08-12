/**
 * `/api/v1/members` — request schemas and calls.
 *
 * Schemas mirror section 5 of `.ai/api-requests.xsd`; the role ↔ department rule additionally
 * mirrors `RoleRules.IsOrgRole` on the server, which the XSD cannot express.
 */

import { z } from 'zod'

import { isOrgRole, SYSTEM_ROLES, type DepartmentCode, type MemberSort, type MemberStatus, type SystemRole } from '@/lib/types/enums'
import type { MemberDto, MemberSummaryDto, PagedResult } from '@/lib/types/dto'
import { emailSchema, guidSchema } from '@/lib/validation/common'

import { apiFetch, type QueryParams } from './client'

// ---------------------------------------------------------------- filters

/** Query for `GET /members`. Every field is optional; empty values are dropped by the client. */
export interface MemberFilters {
  department?: DepartmentCode | ''
  role?: SystemRole | ''
  status?: MemberStatus | ''
  search?: string
  sort?: MemberSort | ''
  page?: number
  pageSize?: number
}

// ---------------------------------------------------------------- schemas

/** XSD `PhoneNumber` — enforced by the server only on `PUT /members/me`. */
const phoneSchema = z
  .string()
  .trim()
  .max(30, 'Телефонът не може да е по-дълъг от 30 символа.')
  .regex(/^\+?[0-9\s-]{6,20}$/, 'Телефонът трябва да е между 6 и 20 цифри.')
  .or(z.literal(''))

/** XSD `UpdateMyProfileRequest` — the only field a member may change about themselves. */
export const updateMyProfileSchema = z.object({ phoneNumber: phoneSchema })
export type UpdateMyProfileForm = z.infer<typeof updateMyProfileSchema>

const memberFieldsSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(2, 'Името трябва да е поне 2 символа.')
    .max(120, 'Името не може да е по-дълго от 120 символа.'),
  role: z.enum(SYSTEM_ROLES),
  /** Empty string means "no department" — `<select>` cannot hold null. */
  departmentId: z.union([guidSchema, z.literal('')]),
  joinedOn: z.string().min(1, 'Датата на присъединяване е задължителна.'),
  // Not validated for format here: the server accepts any string on the admin endpoints.
  phoneNumber: z.string().trim().max(256, 'Телефонът е твърде дълъг.'),
})

/**
 * The server rejects an org role carrying a department, and a non-org role without one.
 * Checked client-side so the form can point at the offending field instead of surfacing a 400.
 */
function checkRoleDepartmentMatch(
  values: { role: SystemRole; departmentId: string },
  context: z.RefinementCtx,
) {
  if (isOrgRole(values.role) && values.departmentId !== '') {
    context.addIssue({
      code: 'custom',
      path: ['departmentId'],
      message: 'Организационните роли не могат да имат отдел.',
    })
  }

  if (!isOrgRole(values.role) && values.departmentId === '') {
    context.addIssue({
      code: 'custom',
      path: ['departmentId'],
      message: 'Тази роля изисква отдел.',
    })
  }
}

/** XSD `CreateMemberRequest`. */
export const createMemberSchema = memberFieldsSchema
  .extend({ email: emailSchema })
  .superRefine(checkRoleDepartmentMatch)
export type CreateMemberForm = z.infer<typeof createMemberSchema>

/**
 * XSD `UpdateMemberRequest`. The email is **not** editable through `PUT /members/{id}`, but
 * the field is kept in the shape — unvalidated and never sent — so a single form component
 * can serve both create and edit with one resolver type.
 */
export const updateMemberSchema = memberFieldsSchema
  .extend({ email: z.string() })
  .superRefine(checkRoleDepartmentMatch)
export type UpdateMemberForm = z.infer<typeof updateMemberSchema>

/**
 * Turns form values into the JSON body the API expects: empty strings become null, because
 * the server distinguishes "absent" from "empty".
 */
function toRequestBody(values: CreateMemberForm | UpdateMemberForm) {
  return {
    fullName: values.fullName,
    phoneNumber: values.phoneNumber === '' ? null : values.phoneNumber,
    role: values.role,
    departmentId: values.departmentId === '' ? null : values.departmentId,
    joinedOn: values.joinedOn,
  }
}

// ---------------------------------------------------------------- calls

/** `GET /members` — paginated and filterable. */
export function getMembers(filters: MemberFilters): Promise<PagedResult<MemberSummaryDto>> {
  return apiFetch<PagedResult<MemberSummaryDto>>('/members', { query: filters as QueryParams })
}

/** `GET /members/{id}`. */
export function getMember(id: string): Promise<MemberDto> {
  return apiFetch<MemberDto>(`/members/${id}`)
}

/** `GET /members/me`. */
export function getMyProfile(): Promise<MemberDto> {
  return apiFetch<MemberDto>('/members/me')
}

/** `PUT /members/me` — an empty phone number clears the stored value. */
export function updateMyProfile(values: UpdateMyProfileForm): Promise<MemberDto> {
  return apiFetch<MemberDto>('/members/me', {
    method: 'PUT',
    body: { phoneNumber: values.phoneNumber === '' ? null : values.phoneNumber },
  })
}

/** `PUT /members/me/photo` — multipart, field name `file`; images only, ≤ 5 MB. */
export function updateMyPhoto(file: File): Promise<MemberDto> {
  const formData = new FormData()
  formData.append('file', file)
  return apiFetch<MemberDto>('/members/me/photo', { method: 'PUT', formData })
}

/** `POST /members` — creates the account and emails a temporary password. */
export function createMember(values: CreateMemberForm): Promise<MemberDto> {
  return apiFetch<MemberDto>('/members', {
    method: 'POST',
    body: { ...toRequestBody(values), email: values.email },
  })
}

/** `PUT /members/{id}`. */
export function updateMember(id: string, values: UpdateMemberForm): Promise<MemberDto> {
  return apiFetch<MemberDto>(`/members/${id}`, { method: 'PUT', body: toRequestBody(values) })
}

/** `POST /members/{id}/deactivate` — revokes tokens and rotates the security stamp. */
export function deactivateMember(id: string): Promise<void> {
  return apiFetch<void>(`/members/${id}/deactivate`, { method: 'POST' })
}

/** `POST /members/{id}/reactivate`. */
export function reactivateMember(id: string): Promise<void> {
  return apiFetch<void>(`/members/${id}/reactivate`, { method: 'POST' })
}
