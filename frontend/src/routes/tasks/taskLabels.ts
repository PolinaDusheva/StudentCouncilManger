/**
 * Bulgarian labels and badge tones for the task enums. Presentation choices live here rather
 * than in `enums.ts`, so colour is not treated as part of the API contract.
 */

import type { BadgeTone } from '@/components/ui/Badge'
import type { TaskPriority, TaskScope, TaskStatus } from '@/lib/types/enums'

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  New: 'Нова',
  InProgress: 'В процес',
  InReview: 'За проверка',
  Completed: 'Завършена',
  Cancelled: 'Отказана',
}

export const TASK_PRIORITY_LABELS: Record<TaskPriority, string> = {
  Low: 'Нисък',
  Medium: 'Среден',
  High: 'Висок',
  Critical: 'Критичен',
}

export const TASK_SCOPE_LABELS: Record<TaskScope, string> = {
  Organizational: 'Организационна',
  Departmental: 'На отдел',
}

export const TASK_STATUS_TONES: Record<TaskStatus, BadgeTone> = {
  New: 'neutral',
  InProgress: 'info',
  InReview: 'warning',
  Completed: 'success',
  Cancelled: 'neutral',
}

export const TASK_PRIORITY_TONES: Record<TaskPriority, BadgeTone> = {
  Low: 'neutral',
  Medium: 'info',
  High: 'warning',
  Critical: 'danger',
}
