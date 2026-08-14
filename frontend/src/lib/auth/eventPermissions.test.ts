import { describe, expect, it } from 'vitest'

import type { SystemRole } from '@/lib/types/enums'

import {
  canCreateEvent,
  canDeleteEvent,
  canEditEvent,
  type EventActor,
  type EventLike,
} from './eventPermissions'

const MY_DEPT = 'PR' as const
const OTHER_DEPT = 'Sports' as const

const actor = (role: SystemRole, overrides: Partial<EventActor> = {}): EventActor => ({
  id: 'me',
  role,
  department: MY_DEPT,
  ...overrides,
})

const event = (overrides: Partial<EventLike> = {}): EventLike => ({
  department: MY_DEPT,
  organizer: { id: 'someone-else' },
  isDeadline: false,
  ...overrides,
})

describe('canCreateEvent', () => {
  it.each([
    'DeptSecretary',
    'DeptVicePresident',
    'DeptPresident',
    'OrgSecretary',
    'OrgVicePresident',
    'OrgPresident',
  ] as const)('%s може да създава събития', (role) => {
    expect(canCreateEvent(actor(role))).toBe(true)
  })

  it('обикновен член не може', () => {
    expect(canCreateEvent(actor('Member'))).toBe(false)
  })
})

describe('canEditEvent', () => {
  it('организаторът редактира своето събитие, независимо от ролята', () => {
    const mine = event({ organizer: { id: 'me' } })
    expect(canEditEvent(mine, actor('DeptSecretary'))).toBe(true)
    expect(canEditEvent(mine, actor('Member'))).toBe(true)
  })

  it.each(['OrgPresident', 'OrgVicePresident'] as const)('%s редактира всяко събитие', (role) => {
    expect(canEditEvent(event({ department: OTHER_DEPT }), actor(role, { department: null }))).toBe(
      true,
    )
  })

  it.each(['DeptPresident', 'DeptVicePresident'] as const)(
    '%s редактира само събития на своя отдел',
    (role) => {
      expect(canEditEvent(event(), actor(role))).toBe(true)
      expect(canEditEvent(event({ department: OTHER_DEPT }), actor(role))).toBe(false)
    },
  )

  it('чужд секретар не редактира', () => {
    expect(canEditEvent(event(), actor('DeptSecretary'))).toBe(false)
    expect(canEditEvent(event(), actor('OrgSecretary', { department: null }))).toBe(false)
  })

  it('краен срок на задача не се редактира от календара', () => {
    const deadline = event({ isDeadline: true, organizer: { id: 'me' } })
    expect(canEditEvent(deadline, actor('OrgPresident', { department: null }))).toBe(false)
  })
})

describe('canDeleteEvent', () => {
  // Асиметрията спрямо редакцията е нарочна в EventAccess.CanDelete.
  it('организаторът НЕ може да изтрие само защото го е създал', () => {
    const mine = event({ organizer: { id: 'me' } })
    expect(canEditEvent(mine, actor('DeptSecretary'))).toBe(true)
    expect(canDeleteEvent(mine, actor('DeptSecretary'))).toBe(false)
  })

  it.each(['OrgPresident', 'OrgVicePresident'] as const)('%s изтрива всяко събитие', (role) => {
    expect(canDeleteEvent(event({ department: OTHER_DEPT }), actor(role, { department: null }))).toBe(
      true,
    )
  })

  it.each(['DeptPresident', 'DeptVicePresident'] as const)(
    '%s изтрива само събития на своя отдел',
    (role) => {
      expect(canDeleteEvent(event(), actor(role))).toBe(true)
      expect(canDeleteEvent(event({ department: OTHER_DEPT }), actor(role))).toBe(false)
    },
  )

  it('краен срок на задача не се изтрива от календара', () => {
    expect(canDeleteEvent(event({ isDeadline: true }), actor('OrgPresident', { department: null }))).toBe(
      false,
    )
  })
})
