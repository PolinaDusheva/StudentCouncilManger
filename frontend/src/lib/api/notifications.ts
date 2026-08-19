/**
 * `/api/v1/notifications` — the caller's own in-app notifications. No policy: every
 * authenticated member reads and marks only their own (`MarkNotificationRead` 404s otherwise).
 *
 * There is no dedicated unread-count endpoint; the bell reads `totalCount` from
 * `GET /notifications?unreadOnly=true&pageSize=1` instead of fetching full rows for a number.
 */

import type { NotificationDto, PagedResult } from '@/lib/types/dto'

import { apiFetch } from './client'

export interface NotificationFilters {
  unreadOnly?: boolean
  page?: number
  pageSize?: number
}

export function getNotifications(filters: NotificationFilters): Promise<PagedResult<NotificationDto>> {
  return apiFetch<PagedResult<NotificationDto>>('/notifications', {
    query: filters as Record<string, string | number | boolean | undefined>,
  })
}

export function markNotificationRead(id: string): Promise<void> {
  return apiFetch<void>(`/notifications/${id}/read`, { method: 'POST' })
}

export function markAllNotificationsRead(): Promise<void> {
  return apiFetch<void>('/notifications/read-all', { method: 'POST' })
}

/** Where a notification's payload should navigate to, or null for one with no target. */
export function notificationTarget(notification: NotificationDto): string | null {
  const payload = notification.payload
  if (!payload) return null

  if (payload.type === 'Task' && payload.id) return `/tasks/${payload.id}`
  if (payload.type === 'Event' && payload.id) return `/events/${payload.id}`
  // A duty reminder deep-links to the caller's own summary — not modelled yet (module 5 is
  // deferred), so it is treated as having no navigable target for now.
  return null
}
