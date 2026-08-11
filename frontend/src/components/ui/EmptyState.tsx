import type { ReactNode } from 'react'
import { Inbox } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

interface EmptyStateProps {
  title: string
  description?: string
  /** Optional call to action, e.g. an "Add member" button. */
  action?: ReactNode
  className?: string
}

/** Shown in place of a list or table that has no rows. */
export function EmptyState({ title, description, action, className }: EmptyStateProps) {
  return (
    <div className={cn('flex flex-col items-center px-4 py-12 text-center', className)}>
      <Inbox aria-hidden className="mb-3 size-8 text-slate-300" />
      <p className="text-sm font-medium text-slate-900">{title}</p>
      {description && <p className="mt-1 max-w-sm text-sm text-slate-500">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}
