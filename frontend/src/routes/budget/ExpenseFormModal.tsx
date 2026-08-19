import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Modal } from '@/components/ui/Modal'
import { expenseSchema, type ExpenseForm } from '@/lib/api/budget'
import { errorMessage } from '@/lib/api/problem'
import type { ExpenseDto } from '@/lib/types/dto'

interface ExpenseFormModalProps {
  open: boolean
  /** Present when editing; absent for a new expense. */
  expense?: ExpenseDto | null
  onClose: () => void
  onSubmit: (values: ExpenseForm) => void
  submitting: boolean
  error: unknown
}

function todayLocal(): string {
  const now = new Date()
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10)
}

/** Create/edit as a modal rather than a route: the form is three fields, a page would be empty. */
export function ExpenseFormModal({
  open,
  expense,
  onClose,
  onSubmit,
  submitting,
  error,
}: ExpenseFormModalProps) {
  const isEdit = expense != null

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ExpenseForm>({
    resolver: zodResolver(expenseSchema),
    defaultValues: { description: '', amountEur: '', spentOn: todayLocal() },
  })

  useEffect(() => {
    if (!open) return
    reset(
      expense
        ? { description: expense.description, amountEur: String(expense.amountEur), spentOn: expense.spentOn }
        : { description: '', amountEur: '', spentOn: todayLocal() },
    )
  }, [open, expense, reset])

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={isEdit ? 'Редакция на разход' : 'Нов разход'}
      footer={
        <>
          <Button type="button" variant="secondary" onClick={onClose} disabled={submitting}>
            Отказ
          </Button>
          <Button type="submit" form="expense-form" loading={submitting}>
            {isEdit ? 'Запази' : 'Добави'}
          </Button>
        </>
      }
    >
      <form id="expense-form" onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        {error != null && <Alert tone="error">{errorMessage(error)}</Alert>}

        <Input
          label="Описание"
          autoFocus
          error={errors.description?.message}
          {...register('description')}
        />

        <Input
          label="Сума (€)"
          type="number"
          step="0.01"
          min="0.01"
          inputMode="decimal"
          error={errors.amountEur?.message}
          {...register('amountEur')}
        />

        <Input
          label="Дата"
          type="date"
          max={todayLocal()}
          hint="Не може да е в бъдещето."
          error={errors.spentOn?.message}
          {...register('spentOn')}
        />
      </form>
    </Modal>
  )
}
