import { describe, expect, it } from 'vitest'

import type { TaskDetailDto } from '@/lib/types/dto'
import type { SystemRole, TaskScope, TaskStatus } from '@/lib/types/enums'

import {
  allowedTransitions,
  canCancelTask,
  canCreateScope,
  canDeleteTask,
  canEditTask,
  canChangeTaskStatus,
  type TaskActor,
} from './taskPermissions'

const MY_DEPT = 'PR' as const
const OTHER_DEPT = 'Sports' as const

const actor = (role: SystemRole, overrides: Partial<TaskActor> = {}): TaskActor => ({
  id: 'me',
  role,
  department: MY_DEPT,
  ...overrides,
})

/** Minimal task shape; only the fields the permission rules read. */
const task = (
  scope: TaskScope,
  overrides: Partial<Pick<TaskDetailDto, 'department' | 'status' | 'assignees'>> = {},
) =>
  ({
    scope,
    department: scope === 'Departmental' ? MY_DEPT : null,
    status: 'New' as TaskStatus,
    assignees: [],
    ...overrides,
  }) as Pick<TaskDetailDto, 'scope' | 'department' | 'status' | 'assignees'>

describe('canEditTask', () => {
  it.each(['OrgPresident', 'OrgVicePresident'] as const)('%s редактира всяка задача', (role) => {
    expect(canEditTask(task('Organizational'), actor(role, { department: null }))).toBe(true)
    expect(
      canEditTask(task('Departmental', { department: OTHER_DEPT }), actor(role, { department: null })),
    ).toBe(true)
  })

  // OrgSecretary вижда всичко (TaskAccess.IsOrg), но НЕ е в CanEdit — лесно се пропуска.
  it('OrgSecretary не редактира нищо, макар да вижда всичко', () => {
    expect(canEditTask(task('Organizational'), actor('OrgSecretary', { department: null }))).toBe(false)
    expect(canEditTask(task('Departmental'), actor('OrgSecretary', { department: null }))).toBe(false)
  })

  it.each(['DeptPresident', 'DeptVicePresident'] as const)(
    '%s редактира само департаментна задача на своя отдел',
    (role) => {
      expect(canEditTask(task('Departmental'), actor(role))).toBe(true)
      expect(canEditTask(task('Departmental', { department: OTHER_DEPT }), actor(role))).toBe(false)
      expect(canEditTask(task('Organizational'), actor(role))).toBe(false)
    },
  )

  it.each(['DeptSecretary', 'Member'] as const)('%s не редактира нищо', (role) => {
    expect(canEditTask(task('Departmental'), actor(role))).toBe(false)
    expect(canEditTask(task('Organizational'), actor(role))).toBe(false)
  })
})

describe('canCancelTask', () => {
  it('съвпада с правото за редакция', () => {
    expect(canCancelTask(task('Departmental'), actor('DeptPresident'))).toBe(true)
    expect(canCancelTask(task('Departmental'), actor('DeptSecretary'))).toBe(false)
  })
})

describe('canDeleteTask', () => {
  it('само OrgPresident', () => {
    expect(canDeleteTask(actor('OrgPresident', { department: null }))).toBe(true)
    expect(canDeleteTask(actor('OrgVicePresident', { department: null }))).toBe(false)
    expect(canDeleteTask(actor('DeptPresident'))).toBe(false)
  })
})

describe('canCreateScope', () => {
  it.each(['OrgPresident', 'OrgVicePresident'] as const)('%s създава и двата вида', (role) => {
    const me = actor(role, { department: null })
    expect(canCreateScope('Organizational', me)).toBe(true)
    expect(canCreateScope('Departmental', me)).toBe(true)
  })

  it('OrgSecretary не създава нищо', () => {
    const me = actor('OrgSecretary', { department: null })
    expect(canCreateScope('Organizational', me)).toBe(false)
    expect(canCreateScope('Departmental', me)).toBe(false)
  })

  it.each(['DeptPresident', 'DeptVicePresident'] as const)(
    '%s създава само департаментни',
    (role) => {
      expect(canCreateScope('Organizational', actor(role))).toBe(false)
      expect(canCreateScope('Departmental', actor(role))).toBe(true)
    },
  )

  it.each(['DeptSecretary', 'Member'] as const)('%s не създава нищо', (role) => {
    expect(canCreateScope('Organizational', actor(role))).toBe(false)
    expect(canCreateScope('Departmental', actor(role))).toBe(false)
  })
})

describe('canChangeTaskStatus', () => {
  it('изпълнителят може, дори без право на редакция', () => {
    const me = actor('Member')
    const assigned = task('Departmental', { assignees: [{ id: 'me' }] as never })
    expect(canChangeTaskStatus(assigned, me)).toBe(true)
  })

  it('който може да редактира, може и без да е изпълнител', () => {
    expect(canChangeTaskStatus(task('Departmental'), actor('DeptPresident'))).toBe(true)
  })

  it('страничен наблюдател не може', () => {
    expect(canChangeTaskStatus(task('Departmental'), actor('DeptSecretary'))).toBe(false)
    expect(canChangeTaskStatus(task('Organizational'), actor('OrgSecretary', { department: null }))).toBe(
      false,
    )
  })
})

describe('allowedTransitions', () => {
  it('изпълнителят върви само една стъпка напред', () => {
    const me = actor('Member')
    const assigned = (status: TaskStatus) =>
      task('Departmental', { status, assignees: [{ id: 'me' }] as never })

    expect(allowedTransitions(assigned('New'), me)).toEqual(['InProgress'])
    expect(allowedTransitions(assigned('InProgress'), me)).toEqual(['InReview'])
    // Няма преход напред от InReview за изпълнител — одобрява ръководството.
    expect(allowedTransitions(assigned('InReview'), me)).toEqual([])
    expect(allowedTransitions(assigned('Completed'), me)).toEqual([])
  })

  it('ръководството може към всеки друг статус', () => {
    const me = actor('DeptPresident')
    const options = allowedTransitions(task('Departmental', { status: 'InReview' }), me)

    expect(options).toContain('Completed')
    expect(options).toContain('New')
    // Текущият статус не е опция — сървърът връща 409 за преход към същия.
    expect(options).not.toContain('InReview')
  })

  it('не предлага нищо на страничен наблюдател', () => {
    expect(allowedTransitions(task('Departmental'), actor('DeptSecretary'))).toEqual([])
  })

  it('не предлага Cancelled — за това има отделен идемпотентен ендпойнт', () => {
    const options = allowedTransitions(task('Departmental'), actor('OrgPresident', { department: null }))
    expect(options).not.toContain('Cancelled')
  })
})
