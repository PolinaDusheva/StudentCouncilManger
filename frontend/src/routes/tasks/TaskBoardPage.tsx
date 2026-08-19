import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { List, Plus } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { changeTaskStatus, getTaskBoard } from '@/lib/api/tasks'
import { canCreateAnyTask, toTaskActor } from '@/lib/auth/taskPermissions'
import { useAuth } from '@/lib/auth/useAuth'
import type { TaskBoardDto, TaskListItemDto } from '@/lib/types/dto'
import type { TaskStatus } from '@/lib/types/enums'

import { TaskCard } from './TaskCard'
import { TASK_STATUS_LABELS } from './taskLabels'

/**
 * The four board columns. `Cancelled` is deliberately absent — the server excludes cancelled
 * tasks from this endpoint entirely.
 */
const COLUMNS: { status: TaskStatus; key: keyof TaskBoardDto['columns'] }[] = [
  { status: 'New', key: 'new' },
  { status: 'InProgress', key: 'inProgress' },
  { status: 'InReview', key: 'inReview' },
  { status: 'Completed', key: 'completed' },
]

export function TaskBoardPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [changingId, setChangingId] = useState<string | null>(null)

  const { data, isPending, isError, error } = useQuery({
    queryKey: queryKeys.tasks.board(),
    queryFn: getTaskBoard,
  })

  const changeStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: TaskStatus }) => changeTaskStatus(id, status),
    onSettled: async () => {
      setChangingId(null)
      await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all })
    },
  })

  const actor = user ? toTaskActor(user) : null

  if (isPending) return <Spinner />
  if (isError) return <Alert tone="error">{errorMessage(error)}</Alert>

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Дъска</h1>

        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => navigate('/tasks')}>
            <List aria-hidden className="size-4" />
            Списък
          </Button>

          {actor && canCreateAnyTask(actor) && (
            <Button onClick={() => navigate('/tasks/new')}>
              <Plus aria-hidden className="size-4" />
              Нова задача
            </Button>
          )}
        </div>
      </div>

      {changeStatus.isError && <Alert tone="error">{errorMessage(changeStatus.error)}</Alert>}

      {/* The endpoint takes no parameters, so there is nothing to filter by here. */}
      <p className="text-sm text-muted">
        Показват се всички видими задачи без отказаните. За филтри използвай списъка.
      </p>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {COLUMNS.map(({ status, key }) => (
          <Column
            key={key}
            title={TASK_STATUS_LABELS[status]}
            tasks={data.columns[key]}
            actor={actor}
            changingId={changingId}
            onStatusChange={(id, next) => {
              setChangingId(id)
              changeStatus.mutate({ id, status: next })
            }}
          />
        ))}
      </div>
    </div>
  )
}

function Column({
  title,
  tasks,
  actor,
  changingId,
  onStatusChange,
}: {
  title: string
  tasks: TaskListItemDto[]
  actor: ReturnType<typeof toTaskActor> | null
  changingId: string | null
  onStatusChange: (id: string, status: TaskStatus) => void
}) {
  return (
    <section className="rounded-[20px] bg-page p-3">
      <h2 className="mb-3 flex items-center justify-between px-1 text-sm font-bold text-ink-soft">
        {title}
        <span className="rounded-full bg-surface px-2 py-0.5 text-xs font-normal text-muted">
          {tasks.length}
        </span>
      </h2>

      {tasks.length === 0 ? (
        <p className="px-1 py-6 text-center text-sm text-faint">Няма задачи</p>
      ) : (
        <ul className="space-y-2">
          {tasks.map((task) => (
            <li key={task.id}>
              {actor && (
                <TaskCard
                  task={task}
                  actor={actor}
                  changing={changingId === task.id}
                  onStatusChange={(status) => onStatusChange(task.id, status)}
                />
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
