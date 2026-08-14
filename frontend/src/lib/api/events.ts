/**
 * `/api/v1/events` — request schemas and calls.
 *
 * Mirrors section 8 of `.ai/api-requests.xsd`. Three server behaviours shape the client:
 *
 * - The response mixes three kinds of entry: real events, expanded occurrences of a recurring
 *   event (`occurrenceStartUtc`), and **virtual task deadlines** (`isDeadline`, `taskId`).
 *   Only the first kind is editable.
 * - `view` resolves the `[from, to)` window when the bounds are missing; explicit bounds win.
 * - Creating or updating returns overlapping events as a **warning**, not an error — the event
 *   is already saved by the time the conflicts are reported.
 */

import { z } from 'zod'

import type { EventDetailDto, EventDto, EventMutationResult } from '@/lib/types/dto'
import { EVENT_TYPES, RECURRENCE_TYPES, type CalendarView, type DepartmentCode, type EventType } from '@/lib/types/enums'
import { guidSchema } from '@/lib/validation/common'

import { apiFetch, buildQueryString, type QueryParams } from './client'

/** Query for `GET /events` and `GET /events/export.ics`. */
export interface EventFilters {
  /** ISO-8601 UTC. When both bounds are given, `view` is ignored. */
  from?: string
  to?: string
  type?: EventType | ''
  department?: DepartmentCode | ''
  view?: CalendarView
}

// ---------------------------------------------------------------- schemas

const eventFieldsSchema = z.object({
  title: z
    .string()
    .trim()
    .min(3, 'Заглавието трябва да е поне 3 символа.')
    .max(150, 'Заглавието не може да е по-дълго от 150 символа.'),
  description: z.string().trim().max(4000, 'Описанието не може да е по-дълго от 4000 символа.'),
  location: z.string().trim().max(300, 'Мястото не може да е по-дълго от 300 символа.'),
  type: z.enum(EVENT_TYPES),
  recurrence: z.enum(RECURRENCE_TYPES),
  /** Local wall-clock values from `<input type="datetime-local">`; converted to UTC on send. */
  startLocal: z.string().min(1, 'Началото е задължително.'),
  endLocal: z.string().min(1, 'Краят е задължителен.'),
  /** Empty string means "organisation-wide" — an event needs no department. */
  departmentId: z.union([guidSchema, z.literal('')]),
  participantIds: z.array(guidSchema),
})

export const eventFormSchema = eventFieldsSchema.superRefine((values, context) => {
  const start = new Date(values.startLocal)
  const end = new Date(values.endLocal)

  if (Number.isNaN(start.getTime())) {
    context.addIssue({ code: 'custom', path: ['startLocal'], message: 'Невалидна дата.' })
    return
  }
  if (Number.isNaN(end.getTime())) {
    context.addIssue({ code: 'custom', path: ['endLocal'], message: 'Невалидна дата.' })
    return
  }

  if (end <= start) {
    context.addIssue({
      code: 'custom',
      path: ['endLocal'],
      message: 'Краят трябва да е след началото.',
    })
  }
})
export type EventForm = z.infer<typeof eventFormSchema>

function toRequestBody(values: EventForm) {
  return {
    title: values.title,
    description: values.description === '' ? null : values.description,
    startUtc: new Date(values.startLocal).toISOString(),
    endUtc: new Date(values.endLocal).toISOString(),
    location: values.location === '' ? null : values.location,
    type: values.type,
    departmentId: values.departmentId === '' ? null : values.departmentId,
    recurrence: values.recurrence,
    participantIds: values.participantIds,
  }
}

// ---------------------------------------------------------------- calls

/** `GET /events` — a plain array, not paginated. */
export function getEvents(filters: EventFilters): Promise<EventDto[]> {
  return apiFetch<EventDto[]>('/events', { query: filters as QueryParams })
}

/** `GET /events/{id}` — 404 here means the event genuinely does not exist. */
export function getEvent(id: string): Promise<EventDetailDto> {
  return apiFetch<EventDetailDto>(`/events/${id}`)
}

/** `POST /events` — returns the saved event plus any overlaps, which are only a warning. */
export function createEvent(values: EventForm): Promise<EventMutationResult> {
  return apiFetch<EventMutationResult>('/events', { method: 'POST', body: toRequestBody(values) })
}

/** `PUT /events/{id}` — same conflict-as-warning contract as create. */
export function updateEvent(id: string, values: EventForm): Promise<EventMutationResult> {
  return apiFetch<EventMutationResult>(`/events/${id}`, {
    method: 'PUT',
    body: toRequestBody(values),
  })
}

export function deleteEvent(id: string): Promise<void> {
  return apiFetch<void>(`/events/${id}`, { method: 'DELETE' })
}

/**
 * Path of the `.ics` export for the current window. Fetched through
 * `downloadAuthenticatedFile` because the endpoint requires a bearer token.
 */
export function icsExportPath(filters: EventFilters): string {
  return `/api/v1/events/export.ics${buildQueryString(filters as QueryParams)}`
}
