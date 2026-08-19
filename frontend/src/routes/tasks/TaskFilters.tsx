import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import type { TaskFilters as Filters } from '@/lib/api/tasks'
import {
  DEPARTMENT_CODES,
  DEPARTMENT_CODE_LABELS,
  TASK_PRIORITIES,
  TASK_SCOPES,
  TASK_STATUSES,
} from '@/lib/types/enums'

import { TASK_PRIORITY_LABELS, TASK_SCOPE_LABELS, TASK_STATUS_LABELS } from './taskLabels'

interface TaskFiltersProps {
  filters: Filters
  /** Receives only the changed keys; the caller merges them and resets the page. */
  onChange: (changes: Partial<Filters>) => void
}

const options = <T extends string>(values: readonly T[], labels: Record<T, string>) =>
  values.map((value) => ({ value, label: labels[value] }))

export function TaskFilters({ filters, onChange }: TaskFiltersProps) {
  return (
    <div className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Select
          label="Статус"
          placeholder="Всички"
          options={options(TASK_STATUSES, TASK_STATUS_LABELS)}
          value={filters.status ?? ''}
          onChange={(event) => onChange({ status: event.target.value as Filters['status'] })}
        />

        <Select
          label="Приоритет"
          placeholder="Всички"
          options={options(TASK_PRIORITIES, TASK_PRIORITY_LABELS)}
          value={filters.priority ?? ''}
          onChange={(event) => onChange({ priority: event.target.value as Filters['priority'] })}
        />

        <Select
          label="Вид"
          placeholder="Всички"
          options={options(TASK_SCOPES, TASK_SCOPE_LABELS)}
          value={filters.scope ?? ''}
          onChange={(event) => onChange({ scope: event.target.value as Filters['scope'] })}
        />

        <Select
          label="Отдел"
          placeholder="Всички"
          options={DEPARTMENT_CODES.map((code) => ({
            value: code,
            label: DEPARTMENT_CODE_LABELS[code],
          }))}
          value={filters.department ?? ''}
          onChange={(event) => onChange({ department: event.target.value as Filters['department'] })}
        />
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <Input
          label="Краен срок от"
          type="date"
          className="w-auto"
          value={filters.from?.slice(0, 10) ?? ''}
          // The API wants an instant; a date alone means "from the start of that day".
          onChange={(event) =>
            onChange({ from: event.target.value ? `${event.target.value}T00:00:00Z` : '' })
          }
        />

        <Input
          label="до"
          type="date"
          className="w-auto"
          value={filters.to?.slice(0, 10) ?? ''}
          onChange={(event) =>
            onChange({ to: event.target.value ? `${event.target.value}T23:59:59Z` : '' })
          }
        />

        <label className="flex h-10 items-center gap-2 text-sm text-ink-soft">
          <input
            type="checkbox"
            checked={filters.overdue ?? false}
            onChange={(event) => onChange({ overdue: event.target.checked || undefined })}
            className="text-accent focus:ring-accent size-4 rounded border-line"
          />
          Само просрочени
        </label>
      </div>
    </div>
  )
}
