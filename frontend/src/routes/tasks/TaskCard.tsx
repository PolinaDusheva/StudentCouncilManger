import { Link } from 'react-router-dom'
import { AlertTriangle, MessageSquare, Paperclip, Users } from 'lucide-react'

import { Badge } from '@/components/ui/Badge'
import type { TaskActor } from '@/lib/auth/taskPermissions'
import type { TaskListItemDto } from '@/lib/types/dto'
import { DEPARTMENT_CODE_LABELS, type TaskStatus } from '@/lib/types/enums'
import { formatDate } from '@/lib/utils/format'

import { TASK_PRIORITY_LABELS, TASK_PRIORITY_TONES } from './taskLabels'
import { TaskStatusMenu } from './TaskStatusMenu'

interface TaskCardProps {
  task: TaskListItemDto
  actor: TaskActor
  onStatusChange: (status: TaskStatus) => void
  changing?: boolean
}

/** One task on the board: enough to recognise it, plus the moves the member may make. */
export function TaskCard({ task, actor, onStatusChange, changing = false }: TaskCardProps) {
  return (
    <article className="rounded-[15px] bg-surface p-3 shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider">
      <Link
        to={`/tasks/${task.id}`}
        className="hover:text-accent-hover block text-sm font-medium text-ink hover:underline"
      >
        {task.title}
      </Link>

      <div className="mt-2 flex flex-wrap items-center gap-1.5">
        <Badge tone={TASK_PRIORITY_TONES[task.priority]}>
          {TASK_PRIORITY_LABELS[task.priority]}
        </Badge>
        {task.department && <Badge>{DEPARTMENT_CODE_LABELS[task.department]}</Badge>}
      </div>

      {task.dueAtUtc && (
        <p
          className={
            task.isOverdue
              ? 'mt-2 flex items-center gap-1 text-xs font-medium text-danger'
              : 'mt-2 text-xs text-muted'
          }
        >
          {task.isOverdue && <AlertTriangle aria-hidden className="size-3.5" />}
          {formatDate(task.dueAtUtc)}
        </p>
      )}

      <div className="mt-3 flex items-center justify-between gap-2">
        <div className="flex items-center gap-2.5 text-xs text-muted">
          <Counter icon={Users} count={task.assigneeCount} label="изпълнители" />
          <Counter icon={MessageSquare} count={task.commentCount} label="коментара" />
          <Counter icon={Paperclip} count={task.documentCount} label="документа" />
        </div>

        <TaskStatusMenu
          task={task}
          actor={actor}
          size="sm"
          loading={changing}
          onChange={onStatusChange}
        />
      </div>
    </article>
  )
}

function Counter({
  icon: Icon,
  count,
  label,
}: {
  icon: typeof Users
  count: number
  label: string
}) {
  if (count === 0) return null

  return (
    <span className="inline-flex items-center gap-1">
      <Icon aria-hidden className="size-3.5" />
      {count}
      <span className="sr-only">{label}</span>
    </span>
  )
}
