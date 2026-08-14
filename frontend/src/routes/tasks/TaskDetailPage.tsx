import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, ArrowLeft, Ban, Pencil, Trash2 } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Spinner } from '@/components/ui/Spinner'
import { ApiError, errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { cancelTask, changeTaskStatus, deleteTask, getTask } from '@/lib/api/tasks'
import {
  canCancelTask,
  canDeleteTask,
  canEditTask,
  toTaskActor,
} from '@/lib/auth/taskPermissions'
import { useAuth } from '@/lib/auth/useAuth'
import type { MemberSummaryDto } from '@/lib/types/dto'
import { DEPARTMENT_CODE_LABELS, type TaskStatus } from '@/lib/types/enums'
import { formatDateTime } from '@/lib/utils/format'

import {
  TASK_PRIORITY_LABELS,
  TASK_PRIORITY_TONES,
  TASK_SCOPE_LABELS,
  TASK_STATUS_LABELS,
  TASK_STATUS_TONES,
} from './taskLabels'
import { TaskComments } from './TaskComments'
import { TaskDocuments } from './TaskDocuments'
import { TaskStatusMenu } from './TaskStatusMenu'

/** Which destructive action the confirmation dialog is currently asking about. */
type PendingAction = 'cancel' | 'delete' | null

export function TaskDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()

  const [pending, setPending] = useState<PendingAction>(null)

  const { data: task, isPending, isError, error } = useQuery({
    queryKey: queryKeys.tasks.detail(id),
    queryFn: () => getTask(id),
    enabled: id !== '',
  })

  const invalidateTasks = () => queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all })

  const changeStatus = useMutation({
    mutationFn: (status: TaskStatus) => changeTaskStatus(id, status),
    onSuccess: async (updated) => {
      queryClient.setQueryData(queryKeys.tasks.detail(id), updated)
      await invalidateTasks()
    },
  })

  const cancel = useMutation({
    mutationFn: () => cancelTask(id),
    onSuccess: async () => {
      setPending(null)
      await invalidateTasks()
    },
  })

  const remove = useMutation({
    mutationFn: () => deleteTask(id),
    onSuccess: async () => {
      setPending(null)
      await invalidateTasks()
      navigate('/tasks', { replace: true })
    },
  })

  if (isPending) return <Spinner />

  if (isError) {
    // The API answers 404 for a task the caller may not see, so existence is never leaked.
    // Saying "no access" here would give away exactly what the server is hiding.
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <div className="space-y-4">
        <BackLink />
        <Alert tone="error">
          {notFound ? 'Задачата не е намерена.' : errorMessage(error)}
        </Alert>
      </div>
    )
  }

  const isOverdue = task.dueAtUtc !== null && task.status !== 'Completed' && task.status !== 'Cancelled'
    ? new Date(task.dueAtUtc) < new Date()
    : false

  const actor = user ? toTaskActor(user) : null
  const canEdit = actor ? canEditTask(task, actor) : false
  const canCancel = actor ? canCancelTask(task, actor) : false
  const canDelete = actor ? canDeleteTask(actor) : false

  return (
    <div className="space-y-5">
      <BackLink />

      <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h1 className="text-xl font-semibold text-slate-900">{task.title}</h1>

          <div className="flex flex-wrap items-center gap-2">
            {actor && (
              <TaskStatusMenu
                task={task}
                actor={actor}
                loading={changeStatus.isPending}
                onChange={(status) => changeStatus.mutate(status)}
              />
            )}

            {canEdit && (
              <Button variant="secondary" onClick={() => navigate(`/tasks/${task.id}/edit`)}>
                <Pencil aria-hidden className="size-4" />
                Редактирай
              </Button>
            )}

            {canCancel && task.status !== 'Cancelled' && (
              <Button variant="secondary" onClick={() => setPending('cancel')}>
                <Ban aria-hidden className="size-4" />
                Откажи
              </Button>
            )}

            {canDelete && (
              <Button variant="danger" onClick={() => setPending('delete')}>
                <Trash2 aria-hidden className="size-4" />
                Изтрий
              </Button>
            )}
          </div>
        </div>

        <div className="mt-3 flex flex-wrap gap-2">
          {/* The status badge is dropped when the menu already shows it, to avoid repeating it. */}
          {!actor && (
            <Badge tone={TASK_STATUS_TONES[task.status]}>{TASK_STATUS_LABELS[task.status]}</Badge>
          )}
          <Badge tone={TASK_PRIORITY_TONES[task.priority]}>
            {TASK_PRIORITY_LABELS[task.priority]}
          </Badge>
          <Badge>{TASK_SCOPE_LABELS[task.scope]}</Badge>
          {task.department && <Badge>{DEPARTMENT_CODE_LABELS[task.department]}</Badge>}
        </div>

        {changeStatus.isError && (
          <Alert tone="error" className="mt-4">
            {errorMessage(changeStatus.error)}
          </Alert>
        )}

        {task.description && (
          <p className="mt-5 border-t border-slate-200 pt-5 text-sm whitespace-pre-wrap text-slate-700">
            {task.description}
          </p>
        )}

        <dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-slate-200 pt-5 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-slate-500">Краен срок</dt>
            <dd className="mt-0.5 flex items-center gap-1.5 font-medium text-slate-900">
              {isOverdue && <AlertTriangle aria-hidden className="size-4 text-red-500" />}
              <span className={isOverdue ? 'text-red-600' : undefined}>
                {formatDateTime(task.dueAtUtc)}
              </span>
            </dd>
          </div>

          <div>
            <dt className="text-slate-500">Създадена</dt>
            <dd className="mt-0.5 font-medium text-slate-900">
              {formatDateTime(task.createdAtUtc)}
              {task.createdBy && (
                <span className="font-normal text-slate-500"> от {task.createdBy.fullName}</span>
              )}
            </dd>
          </div>
        </dl>
      </div>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">
          Изпълнители ({task.assignees.length})
        </h2>

        <ul className="grid gap-3 sm:grid-cols-2">
          {task.assignees.map((assignee) => (
            <li key={assignee.id}>
              <MemberLine member={assignee} />
            </li>
          ))}
        </ul>
      </section>

      <TaskDocuments taskId={task.id} />

      <TaskComments taskId={task.id} />

      {(cancel.isError || remove.isError) && (
        <Alert tone="error">{errorMessage(cancel.error ?? remove.error)}</Alert>
      )}

      <ConfirmDialog
        open={pending === 'cancel'}
        title="Отказ на задача"
        message={`„${task.title}“ ще бъде отбелязана като отказана. Остава видима, но не се работи по нея.`}
        confirmLabel="Откажи задачата"
        loading={cancel.isPending}
        onConfirm={() => cancel.mutate()}
        onCancel={() => setPending(null)}
      />

      <ConfirmDialog
        open={pending === 'delete'}
        title="Изтриване на задача"
        message={`„${task.title}“ ще бъде изтрита завинаги, заедно с коментарите и документите си. Действието е необратимо.`}
        confirmLabel="Изтрий"
        loading={remove.isPending}
        onConfirm={() => remove.mutate()}
        onCancel={() => setPending(null)}
      />
    </div>
  )
}

function BackLink() {
  return (
    <Link
      to="/tasks"
      className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
    >
      <ArrowLeft aria-hidden className="size-4" />
      Всички задачи
    </Link>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-brand-700 truncate text-sm font-medium text-slate-900 group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
