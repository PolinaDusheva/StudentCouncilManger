import { ChevronLeft, ChevronRight } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

import { Button } from './Button'

interface PaginationProps {
  /** These four mirror `PagedResult<T>` from the API. */
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPageChange: (page: number) => void
  className?: string
}

/** Page-at-a-time navigation for the list endpoints. */
export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  className,
}: PaginationProps) {
  // Nothing to navigate: keep the layout clean rather than showing two dead buttons.
  if (totalPages <= 1) return null

  const first = (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, totalCount)

  return (
    <div className={cn('flex items-center justify-between gap-4 pt-3', className)}>
      <p className="text-sm text-slate-600">
        {first} – {last} от {totalCount}
      </p>

      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Предишна страница"
        >
          <ChevronLeft aria-hidden className="size-4" />
        </Button>

        <span className="text-sm text-slate-600">
          {page} / {totalPages}
        </span>

        <Button
          variant="secondary"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Следваща страница"
        >
          <ChevronRight aria-hidden className="size-4" />
        </Button>
      </div>
    </div>
  )
}
