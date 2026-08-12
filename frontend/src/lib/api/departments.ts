/**
 * `/api/v1/departments` — read-only. Departments are seeded by the server and cannot be
 * created or edited through the API.
 */

import type { DepartmentDetailDto, DepartmentDto } from '@/lib/types/dto'

import { apiFetch } from './client'

/** `GET /departments` — all departments with member counts and leadership. */
export function getDepartments(): Promise<DepartmentDto[]> {
  return apiFetch<DepartmentDto[]>('/departments')
}

/** `GET /departments/{id}` — department details plus its members. */
export function getDepartment(id: string): Promise<DepartmentDetailDto> {
  return apiFetch<DepartmentDetailDto>(`/departments/${id}`)
}
