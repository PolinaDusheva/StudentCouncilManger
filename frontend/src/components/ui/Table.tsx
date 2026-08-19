import type { ReactNode } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

import { EmptyState } from './EmptyState'
import { Spinner } from './Spinner'

export interface Column<T> {
  key: string
  header: string
  render: (row: T) => ReactNode
  /**
   * Field name the server sorts by (see `MemberSort`/`TaskSort` in the XSD). Omit to make
   * the column non-sortable.
   */
  sortKey?: string
  className?: string
}

interface TableProps<T> {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading?: boolean
  /** Replaces the default empty state. */
  empty?: ReactNode
  /** Current sort in the API's format, e.g. `-joinedOn`. */
  sort?: string
  onSortChange?: (sort: string) => void
  /**
   * Convenience for mouse users only — a `<tr>` cannot be focused, so this is unreachable
   * by keyboard. Screens that use it MUST also render a real link or button inside one of
   * the cells (typically the name column) so the row has a keyboard-accessible route.
   */
  onRowClick?: (row: T) => void
}

/**
 * Data table driven by a column list, so screens declare their columns instead of writing
 * markup. Sorting follows the API convention: a `-` prefix reverses the direction.
 */
export function Table<T>({
  columns,
  rows,
  rowKey,
  loading = false,
  empty,
  sort,
  onSortChange,
  onRowClick,
}: TableProps<T>) {
  const sortField = sort?.startsWith('-') ? sort.slice(1) : sort
  const sortDescending = sort?.startsWith('-') ?? false

  const toggleSort = (sortKey: string) => {
    // Same column → flip direction; a new column starts ascending.
    onSortChange?.(sortField === sortKey && !sortDescending ? `-${sortKey}` : sortKey)
  }

  if (loading) {
    return (
      <div className="rounded-[15px] bg-surface ring-1 ring-divider">
        <Spinner />
      </div>
    )
  }

  if (rows.length === 0) {
    return (
      <div className="rounded-[15px] bg-surface ring-1 ring-divider">
        {empty ?? <EmptyState title="Няма записи" />}
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-[15px] bg-surface ring-1 ring-divider">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-divider bg-subtle text-xs font-semibold text-muted uppercase tracking-wide">
          <tr>
            {columns.map((column) => {
              const isSorted = column.sortKey !== undefined && sortField === column.sortKey
              const SortIcon = sortDescending ? ChevronDown : ChevronUp

              return (
                <th
                  key={column.key}
                  scope="col"
                  aria-sort={
                    isSorted ? (sortDescending ? 'descending' : 'ascending') : undefined
                  }
                  className={cn('px-4 py-3 font-medium', column.className)}
                >
                  {column.sortKey && onSortChange ? (
                    <button
                      type="button"
                      onClick={() => toggleSort(column.sortKey!)}
                      className="inline-flex items-center gap-1 hover:text-ink"
                    >
                      {column.header}
                      {isSorted && <SortIcon aria-hidden className="size-3.5" />}
                    </button>
                  ) : (
                    column.header
                  )}
                </th>
              )
            })}
          </tr>
        </thead>

        <tbody className="divide-y divide-divider">
          {rows.map((row) => (
            <tr
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={cn(onRowClick && 'cursor-pointer hover:bg-row-hover')}
            >
              {columns.map((column) => (
                <td key={column.key} className={cn('px-4 py-3', column.className)}>
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
