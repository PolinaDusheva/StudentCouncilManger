import { describe, expect, it } from 'vitest'

import type { NotificationDto } from '@/lib/types/dto'

import { notificationTarget } from './notifications'

const base: NotificationDto = {
  id: 'n-1',
  type: 'TaskAssigned',
  title: 'Възложена задача',
  body: 'text',
  payload: null,
  isRead: false,
  createdAtUtc: '2026-01-01T00:00:00Z',
}

describe('notificationTarget', () => {
  it('води към задача', () => {
    expect(notificationTarget({ ...base, payload: { type: 'Task', id: 't-1' } })).toBe('/tasks/t-1')
  })

  it('води към събитие', () => {
    expect(notificationTarget({ ...base, payload: { type: 'Event', id: 'e-1' } })).toBe(
      '/events/e-1',
    )
  })

  it('няма цел за напомняне за дежурство', () => {
    expect(notificationTarget({ ...base, payload: { type: 'Duty', id: null } })).toBeNull()
  })

  it('няма цел без payload', () => {
    expect(notificationTarget({ ...base, payload: null })).toBeNull()
  })

  it('няма цел, ако липсва id за Task/Event', () => {
    expect(notificationTarget({ ...base, payload: { type: 'Task', id: null } })).toBeNull()
  })
})
