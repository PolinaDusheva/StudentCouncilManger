import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2, Wallet } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { EmptyState } from '@/components/ui/EmptyState'
import { Pagination } from '@/components/ui/Pagination'
import { Select } from '@/components/ui/Select'
import { Table, type Column } from '@/components/ui/Table'
import {
  createExpense,
  deleteExpense,
  getBudgetSummary,
  getExpenses,
  updateExpense,
  type ExpenseForm,
} from '@/lib/api/budget'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { usePermissions } from '@/lib/auth/useAuth'
import type { ExpenseDto } from '@/lib/types/dto'
import { formatDate, formatEur } from '@/lib/utils/format'

import { ExpenseFormModal } from './ExpenseFormModal'

const PAGE_SIZE = 20

/** The last six years plus the current one — expenses are rarely logged further back. */
function recentYears(): number[] {
  const current = new Date().getFullYear()
  return Array.from({ length: 7 }, (_, i) => current - i)
}

type ModalState = { mode: 'create' } | { mode: 'edit'; expense: ExpenseDto } | null

export function BudgetPage() {
  const queryClient = useQueryClient()
  const { canManageBudget } = usePermissions()

  const [year, setYear] = useState<number>(() => new Date().getFullYear())
  const [page, setPage] = useState(1)
  const [modal, setModal] = useState<ModalState>(null)
  const [pendingDelete, setPendingDelete] = useState<ExpenseDto | null>(null)

  const summary = useQuery({
    queryKey: queryKeys.budget.summary(year),
    queryFn: () => getBudgetSummary(year),
  })

  const filters = { year, page, pageSize: PAGE_SIZE }
  const expenses = useQuery({
    queryKey: queryKeys.budget.expenses(filters),
    queryFn: () => getExpenses(filters),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.budget.all })

  const save = useMutation({
    mutationFn: (values: ExpenseForm) =>
      modal?.mode === 'edit' ? updateExpense(modal.expense.id, values) : createExpense(values),
    onSuccess: async () => {
      setModal(null)
      await invalidate()
    },
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteExpense(id),
    onSuccess: async () => {
      setPendingDelete(null)
      await invalidate()
    },
  })

  const columns: Column<ExpenseDto>[] = [
    { key: 'spentOn', header: 'Дата', render: (expense) => formatDate(expense.spentOn) },
    { key: 'description', header: 'Описание', render: (expense) => expense.description },
    {
      key: 'amount',
      header: 'Сума',
      className: 'text-right',
      render: (expense) => <span className="font-medium">{formatEur(expense.amountEur)}</span>,
    },
    {
      key: 'addedBy',
      header: 'Добавен от',
      render: (expense) => expense.addedBy?.fullName ?? '—',
    },
  ]

  if (canManageBudget) {
    columns.push({
      key: 'actions',
      header: '',
      className: 'text-right',
      render: (expense) => (
        <div className="flex justify-end gap-1">
          <Button
            variant="ghost"
            size="sm"
            aria-label={`Редактирай ${expense.description}`}
            onClick={() => setModal({ mode: 'edit', expense })}
          >
            <Pencil aria-hidden className="size-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            aria-label={`Изтрий ${expense.description}`}
            className="text-danger hover:bg-tone-danger-bg"
            onClick={() => setPendingDelete(expense)}
          >
            <Trash2 aria-hidden className="size-4" />
          </Button>
        </div>
      ),
    })
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Бюджет</h1>

        {canManageBudget && (
          <Button onClick={() => setModal({ mode: 'create' })}>
            <Plus aria-hidden className="size-4" />
            Нов разход
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end justify-between gap-3">
        <Card className="flex items-center gap-3 px-5 py-4">
          <span className="bg-accent-soft flex size-10 items-center justify-center rounded-lg">
            <Wallet aria-hidden className="text-accent-soft-text size-5" />
          </span>
          <div>
            <p className="text-xs text-muted">Общо за {year}</p>
            <p className="text-xl font-semibold text-ink">
              {summary.isPending ? '…' : formatEur(summary.data?.totalEur ?? 0)}
            </p>
          </div>
        </Card>

        <Select
          label="Година"
          options={recentYears().map((y) => ({ value: String(y), label: String(y) }))}
          value={String(year)}
          onChange={(event) => {
            setYear(Number(event.target.value))
            setPage(1)
          }}
          className="w-32"
        />
      </div>

      {expenses.isError ? (
        <Alert tone="error">{errorMessage(expenses.error)}</Alert>
      ) : (
        <>
          <Table
            columns={columns}
            rows={expenses.data?.items ?? []}
            rowKey={(expense) => expense.id}
            loading={expenses.isPending}
            empty={<EmptyState title="Няма разходи за тази година" />}
          />

          {expenses.data && (
            <Pagination
              page={expenses.data.page}
              pageSize={expenses.data.pageSize}
              totalCount={expenses.data.totalCount}
              totalPages={expenses.data.totalPages}
              onPageChange={setPage}
            />
          )}
        </>
      )}

      <ExpenseFormModal
        open={modal !== null}
        expense={modal?.mode === 'edit' ? modal.expense : null}
        onClose={() => setModal(null)}
        onSubmit={(values) => save.mutate(values)}
        submitting={save.isPending}
        error={save.error}
      />

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Изтриване на разход"
        message={`„${pendingDelete?.description}“ (${formatEur(pendingDelete?.amountEur)}) ще бъде изтрит завинаги.`}
        confirmLabel="Изтрий"
        loading={remove.isPending}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
        onCancel={() => setPendingDelete(null)}
      />
    </div>
  )
}
