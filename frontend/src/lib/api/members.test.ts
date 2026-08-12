import { describe, expect, it } from 'vitest'

import { createMemberSchema, updateMemberSchema, updateMyProfileSchema } from './members'

const DEPARTMENT_ID = '3f2504e0-4f89-11d3-9a0c-0305e82c3301'

const validMember = {
  fullName: 'Иван Петров',
  email: 'ivan@ue-varna.bg',
  role: 'Member' as const,
  departmentId: DEPARTMENT_ID,
  joinedOn: '2026-03-09',
  phoneNumber: '',
}

/** Field paths carrying an issue, for terser assertions. */
function issuePaths(result: { success: boolean; error?: { issues: { path: PropertyKey[] }[] } }) {
  return result.error?.issues.map((issue) => issue.path.join('.')) ?? []
}

describe('createMemberSchema', () => {
  it('приема валиден член с отдел', () => {
    expect(createMemberSchema.safeParse(validMember).success).toBe(true)
  })

  it('изисква отдел за неорганизационна роля', () => {
    const result = createMemberSchema.safeParse({ ...validMember, departmentId: '' })

    expect(result.success).toBe(false)
    expect(issuePaths(result)).toContain('departmentId')
  })

  it.each(['OrgSecretary', 'OrgVicePresident', 'OrgPresident'] as const)(
    'отхвърля отдел за организационна роля %s',
    (role) => {
      const result = createMemberSchema.safeParse({ ...validMember, role })

      expect(result.success).toBe(false)
      expect(issuePaths(result)).toContain('departmentId')
    },
  )

  it.each(['OrgSecretary', 'OrgVicePresident', 'OrgPresident'] as const)(
    'приема организационна роля %s без отдел',
    (role) => {
      const result = createMemberSchema.safeParse({ ...validMember, role, departmentId: '' })
      expect(result.success).toBe(true)
    },
  )

  it.each(['DeptSecretary', 'DeptVicePresident', 'DeptPresident', 'Member'] as const)(
    'изисква отдел за ролята на отдел %s',
    (role) => {
      const result = createMemberSchema.safeParse({ ...validMember, role, departmentId: '' })
      expect(result.success).toBe(false)
    },
  )

  it('отхвърля твърде кратко име', () => {
    const result = createMemberSchema.safeParse({ ...validMember, fullName: 'И' })

    expect(result.success).toBe(false)
    expect(issuePaths(result)).toContain('fullName')
  })

  it('отхвърля невалиден имейл', () => {
    const result = createMemberSchema.safeParse({ ...validMember, email: 'без-маймунка' })

    expect(result.success).toBe(false)
    expect(issuePaths(result)).toContain('email')
  })
})

// Полето `email` е в схемата само за да могат create и edit да делят една форма — то не се
// валидира и не се изпраща, защото PUT /members/{id} не го приема.
describe('updateMemberSchema', () => {
  it('не валидира формата на имейла', () => {
    const result = updateMemberSchema.safeParse({ ...validMember, email: 'не-е-имейл' })
    expect(result.success).toBe(true)
  })

  it('приема празен имейл', () => {
    expect(updateMemberSchema.safeParse({ ...validMember, email: '' }).success).toBe(true)
  })

  it('налага същото правило роля ↔ отдел', () => {
    const result = updateMemberSchema.safeParse({ ...validMember, role: 'OrgPresident' })

    expect(result.success).toBe(false)
    expect(issuePaths(result)).toContain('departmentId')
  })
})

describe('updateMyProfileSchema', () => {
  it.each(['+359 888 123 456', '0888123456', '088-812-3456'])('приема телефон %s', (phone) => {
    expect(updateMyProfileSchema.safeParse({ phoneNumber: phone }).success).toBe(true)
  })

  it('приема празен телефон — така се изчиства стойността', () => {
    expect(updateMyProfileSchema.safeParse({ phoneNumber: '' }).success).toBe(true)
  })

  it.each(['123', 'не-е-телефон'])('отхвърля невалиден телефон %s', (phone) => {
    expect(updateMyProfileSchema.safeParse({ phoneNumber: phone }).success).toBe(false)
  })
})
