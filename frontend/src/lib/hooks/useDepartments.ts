import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'

import { getDepartments } from '@/lib/api/departments'
import { queryKeys } from '@/lib/api/queryKeys'
import type { DepartmentCode } from '@/lib/types/enums'

/**
 * The department list, which barely ever changes (four seeded rows), so it is cached for the
 * session rather than refetched per screen.
 */
export function useDepartments() {
  return useQuery({
    queryKey: queryKeys.departments.list(),
    queryFn: getDepartments,
    staleTime: Infinity,
  })
}

/**
 * Maps a `DepartmentCode` to its id.
 *
 * Needed because the two ends of the member API disagree: `GET /members/{id}` returns the
 * department as a *code*, while `PUT /members/{id}` expects a *Guid*. The edit form has to
 * translate between them, and only the department list carries both.
 */
export function useDepartmentIdByCode(): Record<DepartmentCode, string> | undefined {
  const { data } = useDepartments()

  return useMemo(() => {
    if (!data) return undefined

    return Object.fromEntries(data.map((department) => [department.code, department.id])) as Record<
      DepartmentCode,
      string
    >
  }, [data])
}
