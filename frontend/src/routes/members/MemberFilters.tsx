import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import type { MemberFilters as Filters } from '@/lib/api/members'
import {
  DEPARTMENT_CODES,
  DEPARTMENT_CODE_LABELS,
  MEMBER_STATUSES,
  MEMBER_STATUS_LABELS,
  SYSTEM_ROLES,
  SYSTEM_ROLE_LABELS,
} from '@/lib/types/enums'

interface MemberFiltersProps {
  filters: Filters
  /** Receives only the changed keys; the caller merges and resets the page. */
  onChange: (changes: Partial<Filters>) => void
  /** Raw search text, kept separate because it is debounced before it reaches `filters`. */
  search: string
  onSearchChange: (search: string) => void
}

const DEPARTMENT_OPTIONS = DEPARTMENT_CODES.map((code) => ({
  value: code,
  label: DEPARTMENT_CODE_LABELS[code],
}))

const ROLE_OPTIONS = SYSTEM_ROLES.map((role) => ({
  value: role,
  label: SYSTEM_ROLE_LABELS[role],
}))

const STATUS_OPTIONS = MEMBER_STATUSES.map((status) => ({
  value: status,
  label: MEMBER_STATUS_LABELS[status],
}))

export function MemberFilters({ filters, onChange, search, onSearchChange }: MemberFiltersProps) {
  return (
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <Input
        label="Търсене"
        type="search"
        placeholder="Име или имейл"
        value={search}
        onChange={(event) => onSearchChange(event.target.value)}
      />

      <Select
        label="Отдел"
        placeholder="Всички"
        options={DEPARTMENT_OPTIONS}
        value={filters.department ?? ''}
        onChange={(event) => onChange({ department: event.target.value as Filters['department'] })}
      />

      <Select
        label="Роля"
        placeholder="Всички"
        options={ROLE_OPTIONS}
        value={filters.role ?? ''}
        onChange={(event) => onChange({ role: event.target.value as Filters['role'] })}
      />

      <Select
        label="Статус"
        // The server defaults to active-only when the filter is absent, so the blank option
        // is labelled for what it actually does rather than "all".
        placeholder="Активни"
        options={STATUS_OPTIONS}
        value={filters.status ?? ''}
        onChange={(event) => onChange({ status: event.target.value as Filters['status'] })}
      />
    </div>
  )
}
