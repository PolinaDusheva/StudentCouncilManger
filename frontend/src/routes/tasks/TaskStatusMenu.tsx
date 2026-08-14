import { useEffect, useRef, useState } from 'react'
import { ChevronDown } from 'lucide-react'

import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { allowedTransitions, type TaskActor } from '@/lib/auth/taskPermissions'
import type { TaskListItemDto } from '@/lib/types/dto'
import type { TaskStatus } from '@/lib/types/enums'
import { cn } from '@/lib/utils/cn'

import { TASK_STATUS_LABELS, TASK_STATUS_TONES } from './taskLabels'

type TaskLike = Pick<TaskListItemDto, 'scope' | 'department' | 'status'> & {
  assignees?: { id: string }[]
}

interface TaskStatusMenuProps {
  task: TaskLike
  actor: TaskActor
  onChange: (status: TaskStatus) => void
  loading?: boolean
  size?: 'sm' | 'md'
}

/**
 * Current status plus the moves this member may make from it.
 *
 * The options come from {@link allowedTransitions}, which mirrors the server's state machine:
 * an assignee may only take the two self-service steps forward, while leadership may move a
 * task anywhere. When there is no legal move the status renders as a plain badge, so the UI
 * never offers an action that would come back as 409 or 403.
 */
export function TaskStatusMenu({
  task,
  actor,
  onChange,
  loading = false,
  size = 'md',
}: TaskStatusMenuProps) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  const options = allowedTransitions(task, actor)

  useEffect(() => {
    if (!open) return

    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  if (options.length === 0) {
    return <Badge tone={TASK_STATUS_TONES[task.status]}>{TASK_STATUS_LABELS[task.status]}</Badge>
  }

  return (
    <div ref={containerRef} className="relative inline-block">
      <Button
        variant="secondary"
        size={size}
        loading={loading}
        onClick={() => setOpen((current) => !current)}
        aria-haspopup="menu"
        aria-expanded={open}
      >
        {TASK_STATUS_LABELS[task.status]}
        <ChevronDown aria-hidden className="size-4" />
      </Button>

      {open && (
        <div
          role="menu"
          aria-label="Смяна на статус"
          className="absolute right-0 z-10 mt-1 min-w-44 rounded-lg bg-white py-1 shadow-lg ring-1 ring-slate-200"
        >
          {options.map((status) => (
            <button
              key={status}
              type="button"
              role="menuitem"
              onClick={() => {
                setOpen(false)
                onChange(status)
              }}
              className={cn(
                'flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-slate-50',
              )}
            >
              <Badge tone={TASK_STATUS_TONES[status]}>{TASK_STATUS_LABELS[status]}</Badge>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
