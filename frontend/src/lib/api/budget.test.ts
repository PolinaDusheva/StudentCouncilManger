import { describe, expect, it } from 'vitest'

import { expenseSchema } from './budget'

const valid = { description: 'Кетъринг за общото събрание', amountEur: '125.50', spentOn: '2026-01-15' }

describe('expenseSchema', () => {
  it('приема валиден разход', () => {
    expect(expenseSchema.safeParse(valid).success).toBe(true)
  })

  it('отхвърля нулева и отрицателна сума', () => {
    expect(expenseSchema.safeParse({ ...valid, amountEur: '0' }).success).toBe(false)
    expect(expenseSchema.safeParse({ ...valid, amountEur: '-5' }).success).toBe(false)
  })

  it('отхвърля повече от 2 знака след десетичната запетая', () => {
    expect(expenseSchema.safeParse({ ...valid, amountEur: '12.345' }).success).toBe(false)
  })

  it('приема 0, 1 и 2 знака след запетаята', () => {
    expect(expenseSchema.safeParse({ ...valid, amountEur: '12' }).success).toBe(true)
    expect(expenseSchema.safeParse({ ...valid, amountEur: '12.5' }).success).toBe(true)
    expect(expenseSchema.safeParse({ ...valid, amountEur: '12.50' }).success).toBe(true)
  })

  it('отхвърля нечислова сума', () => {
    expect(expenseSchema.safeParse({ ...valid, amountEur: 'абв' }).success).toBe(false)
  })

  it('отхвърля празна сума, вместо да я приема като 0', () => {
    // Причината amountEur да е string схема, а не z.coerce.number(): coerce би превърнало
    // празния низ в 0, което минава "> 0" проверката погрешно.
    expect(expenseSchema.safeParse({ ...valid, amountEur: '' }).success).toBe(false)
  })

  // Датите се строят от местни компоненти (година/месец/ден), а не от toISOString(), за да
  // не се различават с изчисляваната в схемата местна дата близо до полунощ UTC.
  it('отхвърля бъдеща дата', () => {
    const now = new Date()
    const tomorrow = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1)
    const value = `${tomorrow.getFullYear()}-${String(tomorrow.getMonth() + 1).padStart(2, '0')}-${String(tomorrow.getDate()).padStart(2, '0')}`
    expect(expenseSchema.safeParse({ ...valid, spentOn: value }).success).toBe(false)
  })

  it('приема днешната дата', () => {
    const now = new Date()
    const value = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
    expect(expenseSchema.safeParse({ ...valid, spentOn: value }).success).toBe(true)
  })

  it('отхвърля празно описание', () => {
    expect(expenseSchema.safeParse({ ...valid, description: '' }).success).toBe(false)
  })
})
