import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, LayoutGrid, MessageSquare, Paperclip, Plus, Users } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Pagination } from '@/components/ui/Pagination'
import { Table, type Column } from '@/components/ui/Table'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { getMyTasks, getTasks, type TaskFilters as Filters } from '@/lib/api/tasks'
import { canCreateAnyTask, toTaskActor } from '@/lib/auth/taskPermissions'
import { useAuth } from '@/lib/auth/useAuth'
import type { TaskListItemDto } from '@/lib/types/dto'
import { DEPARTMENT_CODE_LABELS, type TaskSort } from '@/lib/types/enums'
import { cn } from '@/lib/utils/cn'
import { formatDate } from '@/lib/utils/format'

import { TaskFilters } from './TaskFilters'
import {
  TASK_PRIORITY_LABELS,
  TASK_PRIORITY_TONES,
  TASK_STATUS_LABELS,
  TASK_STATUS_TONES,
} from './taskLabels'

const PAGE_SIZE = 20

type Tab = 'all' | 'mine'

export function TasksPage() {
  const navigate = useNavigate()
  const { user } = useAuth()

  const [tab, setTab] = useState<Tab>('all')
  const [filters, setFilters] = useState<Filters>({ page: 1, pageSize: PAGE_SIZE, sort: 'dueAt' })

  const isMine = tab === 'mine'

  // `GET /tasks` is paginated and filterable; `GET /tasks/mine` returns a plain array and
  // takes no parameters at all, so the two tabs cannot share one query.
  const allQuery = useQuery({
    queryKey: queryKeys.tasks.list(filters),
    queryFn: () => getTasks(filters),
    enabled: !isMine,
  })

  const mineQuery = useQuery({
    queryKey: queryKeys.tasks.mine(),
    queryFn: getMyTasks,
    enabled: isMine,
  })

  const { isPending, isError, error } = isMine ? mineQuery : allQuery
  const rows = isMine ? (mineQuery.data ?? []) : (allQuery.data?.items ?? [])
  const totalCount = isMine ? (mineQuery.data?.length ?? 0) : (allQuery.data?.totalCount ?? 0)

  const applyFilters = (changes: Partial<Filters>) =>
    setFilters((current) => ({ ...current, ...changes, page: 1 }))

  const canCreate = user ? canCreateAnyTask(toTaskActor(user)) : false

  const columns: Column<TaskListItemDto>[] = [
    {
      key: 'title',
      header: 'Заглавие',
      sortKey: 'title',
      render: (task) => (
        <div className="flex items-start gap-2">
          {task.isOverdue && (
            <AlertTriangle
              aria-label="Просрочена"
              className="mt-0.5 size-4 shrink-0 text-danger"
            />
          )}
          <div className="min-w-0">
            {/* A real link, so the row is reachable by keyboard. */}
            <Link
              to={`/tasks/${task.id}`}
              className="hover:text-accent-hover font-medium hover:underline"
              onClick={(event) => event.stopPropagation()}
            >
              {task.title}
            </Link>
            <Counters task={task} />
          </div>
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Статус',
      sortKey: 'status',
      render: (task) => (
        <Badge tone={TASK_STATUS_TONES[task.status]}>{TASK_STATUS_LABELS[task.status]}</Badge>
      ),
    },
    {
      key: 'priority',
      header: 'Приоритет',
      sortKey: 'priority',
      render: (task) => (
        <Badge tone={TASK_PRIORITY_TONES[task.priority]}>
          {TASK_PRIORITY_LABELS[task.priority]}
        </Badge>
      ),
    },
    {
      key: 'department',
      header: 'Отдел',
      render: (task) =>
        task.department ? (
          DEPARTMENT_CODE_LABELS[task.department]
        ) : (
          <span className="text-faint">Организационна</span>
        ),
    },
    {
      key: 'dueAt',
      header: 'Краен срок',
      sortKey: 'dueAt',
      render: (task) => (
        <span className={cn(task.isOverdue && 'font-medium text-danger')}>
          {formatDate(task.dueAtUtc)}
        </span>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Задачи</h1>
          {!isPending && (
            <p className="mt-1 text-sm text-muted">
              {totalCount} {totalCount === 1 ? 'задача' : 'задачи'}
            </p>
          )}
        </div>

        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => navigate('/tasks/board')}>
            <LayoutGrid aria-hidden className="size-4" />
            Дъска
          </Button>

          {canCreate && (
            <Button onClick={() => navigate('/tasks/new')}>
              <Plus aria-hidden className="size-4" />
              Нова задача
            </Button>
          )}
        </div>
      </div>

      <div role="tablist" aria-label="Обхват" className="flex gap-1 border-b border-divider">
        <TabButton active={tab === 'all'} onClick={() => setTab('all')}>
          Всички
        </TabButton>
        <TabButton active={tab === 'mine'} onClick={() => setTab('mine')}>
          Моите задачи
        </TabButton>
      </div>

      {/* Hidden on "mine": the endpoint ignores every parameter, so showing controls that
          do nothing would be misleading. */}
      {!isMine && <TaskFilters filters={filters} onChange={applyFilters} />}

      {isError ? (
        <Alert tone="error">{errorMessage(error)}</Alert>
      ) : (
        <>
          <Table
            columns={columns}
            rows={rows}
            rowKey={(task) => task.id}
            loading={isPending}
            sort={isMine ? undefined : filters.sort || undefined}
            onSortChange={isMine ? undefined : (sort) => applyFilters({ sort: sort as TaskSort })}
            onRowClick={(task) => navigate(`/tasks/${task.id}`)}
            empty={
              <EmptyState
                title={isMine ? 'Нямаш възложени задачи' : 'Няма намерени задачи'}
                description={isMine ? undefined : 'Опитай с други филтри.'}
              />
            }
          />

          {!isMine && allQuery.data && (
            <Pagination
              page={allQuery.data.page}
              pageSize={allQuery.data.pageSize}
              totalCount={allQuery.data.totalCount}
              totalPages={allQuery.data.totalPages}
              onPageChange={(page) => setFilters((current) => ({ ...current, page }))}
            />
          )}
        </>
      )}
    </div>
  )
}

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean
  onClick: () => void
  children: string
}) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={cn(
        '-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors',
        active
          ? 'border-accent text-ink'
          : 'border-transparent text-muted hover:text-ink',
      )}
    >
      {children}
    </button>
  )
}

/** Assignee, comment and document counts, shown only when non-zero to keep rows quiet. */
function Counters({ task }: { task: TaskListItemDto }) {
  const items = [
    { icon: Users, count: task.assigneeCount, label: 'изпълнители' },
    { icon: MessageSquare, count: task.commentCount, label: 'коментара' },
    { icon: Paperclip, count: task.documentCount, label: 'документа' },
  ].filter((item) => item.count > 0)

  if (items.length === 0) return null

  return (
    <div className="mt-1 flex items-center gap-3 text-xs text-muted">
      {items.map(({ icon: Icon, count, label }) => (
        <span key={label} className="inline-flex items-center gap-1">
          <Icon aria-hidden className="size-3.5" />
          {count}
          <span className="sr-only">{label}</span>
        </span>
      ))}
    </div>
  )
}
