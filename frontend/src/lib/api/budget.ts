/**
 * `/api/v1/budget` — request schemas and calls.
 *
 * All mutations are behind the `CanManageBudget` policy (org leadership only); everyone with
 * a session may read. Mirrors section 10 of `.ai/api-requests.xsd`.
 */

import { z } from 'zod'

import type { BudgetSummaryDto, ExpenseDto, PagedResult } from '@/lib/types/dto'

import { apiFetch, type QueryParams } from './client'

export interface ExpenseFilters {
  year?: number
  page?: number
  pageSize?: number
}

// ---------------------------------------------------------------- schema

/** Today as `YYYY-MM-DD`, in the browser's local zone — matches `<input type="date">`. */
function todayLocal(): string {
  const now = new Date()
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10)
}

export const expenseSchema = z.object({
  description: z
    .string()
    .trim()
    .min(1, 'Описанието е задължително.')
    .max(300, 'Описанието не може да е по-дълго от 300 символа.'),
  /**
   * A string field bound to a numeric `<input>`, so an empty box is representable — `z.coerce
   * .number()` would turn `''` into `0` and silently pass validation.
   */
  amountEur: z
    .string()
    .min(1, 'Сумата е задължителна.')
    .refine((value) => !Number.isNaN(Number(value)), 'Въведи число.')
    .refine((value) => Number(value) > 0, 'Сумата трябва да е по-голяма от 0.')
    .refine((value) => {
      const decimals = value.split('.')[1]
      return !decimals || decimals.length <= 2
    }, 'Най-много 2 знака след десетичната запетая.'),
  spentOn: z
    .string()
    .min(1, 'Датата е задължителна.')
    // The server rejects a future spend date; checked here so the mismatch surfaces before
    // the request, not after.
    .refine((value) => value <= todayLocal(), 'Датата не може да е в бъдещето.'),
})
export type ExpenseForm = z.infer<typeof expenseSchema>

// ---------------------------------------------------------------- calls

export function getBudgetSummary(year?: number): Promise<BudgetSummaryDto> {
  return apiFetch<BudgetSummaryDto>('/budget/summary', { query: { year } })
}

export function getExpenses(filters: ExpenseFilters): Promise<PagedResult<ExpenseDto>> {
  return apiFetch<PagedResult<ExpenseDto>>('/budget/expenses', { query: filters as QueryParams })
}

export function createExpense(values: ExpenseForm): Promise<ExpenseDto> {
  return apiFetch<ExpenseDto>('/budget/expenses', {
    method: 'POST',
    body: {
      description: values.description,
      amountEur: Number(values.amountEur),
      spentOn: values.spentOn,
    },
  })
}

export function updateExpense(id: string, values: ExpenseForm): Promise<ExpenseDto> {
  return apiFetch<ExpenseDto>(`/budget/expenses/${id}`, {
    method: 'PUT',
    body: {
      description: values.description,
      amountEur: Number(values.amountEur),
      spentOn: values.spentOn,
    },
  })
}

export function deleteExpense(id: string): Promise<void> {
  return apiFetch<void>(`/budget/expenses/${id}`, { method: 'DELETE' })
}
