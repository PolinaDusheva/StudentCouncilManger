import { describe, expect, it } from 'vitest'

import {
  buildMonthGrid,
  groupByLocalDay,
  gridWindow,
  localDayKey,
  mondayIndex,
  monthLabel,
  weekWindow,
} from './calendarGrid'

describe('mondayIndex', () => {
  it('слага понеделник на позиция 0, а неделя на 6', () => {
    // 2026-03-09 е понеделник.
    expect(mondayIndex(new Date(2026, 2, 9))).toBe(0)
    expect(mondayIndex(new Date(2026, 2, 15))).toBe(6)
  })
})

describe('buildMonthGrid', () => {
  const march2026 = buildMonthGrid(2026, 2, new Date(2026, 2, 9))

  it('винаги дава 6 седмици по 7 дни', () => {
    expect(march2026).toHaveLength(6)
    for (const week of march2026) expect(week).toHaveLength(7)
  })

  it('започва от понеделник', () => {
    // 1 март 2026 е неделя, значи мрежата тръгва от 23 февруари.
    const first = march2026[0]![0]!
    expect(mondayIndex(first.date)).toBe(0)
    expect(first.date.getDate()).toBe(23)
    expect(first.date.getMonth()).toBe(1)
    expect(first.inMonth).toBe(false)
  })

  it('маркира кои дни са от месеца', () => {
    const inMonth = march2026.flat().filter((day) => day.inMonth)
    expect(inMonth).toHaveLength(31)
    expect(inMonth[0]!.date.getDate()).toBe(1)
    expect(inMonth.at(-1)!.date.getDate()).toBe(31)
  })

  it('маркира днешния ден точно веднъж', () => {
    const today = march2026.flat().filter((day) => day.isToday)
    expect(today).toHaveLength(1)
    expect(today[0]!.date.getDate()).toBe(9)
  })

  it('се справя с месец, който започва в понеделник', () => {
    // 1 юни 2026 е понеделник — без водещи дни от предишния месец.
    const june = buildMonthGrid(2026, 5)
    expect(june[0]![0]!.date.getDate()).toBe(1)
    expect(june[0]![0]!.inMonth).toBe(true)
  })

  it('се справя с февруари във високосна година', () => {
    const feb2028 = buildMonthGrid(2028, 1)
    const inMonth = feb2028.flat().filter((day) => day.inMonth)
    expect(inMonth).toHaveLength(29)
  })
})

describe('gridWindow', () => {
  it('покрива цялата мрежа, не само месеца', () => {
    const weeks = buildMonthGrid(2026, 2)
    const { from, to } = gridWindow(weeks)

    // Тръгва от 23 февруари, за да се виждат събития и на водещите клетки.
    expect(new Date(from).getDate()).toBe(23)
    expect(new Date(from).getMonth()).toBe(1)

    // Горната граница е изключваща — денят след последната клетка.
    const lastCell = weeks.at(-1)!.at(-1)!.date
    expect(new Date(to).getTime() - lastCell.getTime()).toBe(24 * 60 * 60 * 1000)
  })
})

describe('weekWindow', () => {
  it('тръгва от понеделник и трае 7 дни', () => {
    // 2026-03-11 е сряда.
    const { from, to } = weekWindow(new Date(2026, 2, 11))
    expect(new Date(from).getDate()).toBe(9)
    expect(new Date(to).getTime() - new Date(from).getTime()).toBe(7 * 24 * 60 * 60 * 1000)
  })
})

describe('monthLabel', () => {
  it('дава името на месеца на български', () => {
    expect(monthLabel(2026, 2)).toBe('Март 2026')
    expect(monthLabel(2026, 11)).toBe('Декември 2026')
  })
})

// Ключовете се извеждат от същите моменти, а не се пишат наизуст: групирането е по
// МЕСТЕН ден, така че „09T08:00Z“ пада на 8-и в зона UTC-10. Това е желаното поведение
// за календар и тестът не бива да зависи от зоната на машината.
describe('groupByLocalDay', () => {
  it('слага в една група събитията от един и същ местен ден', () => {
    // Моментите се строят от МЕСТНИ дати, за да е сигурно на кой местен ден падат,
    // независимо от зоната на машината. Часовете 09:00 и 15:00 са далеч от полунощ.
    const morning = new Date(2026, 2, 9, 9).toISOString()
    const afternoon = new Date(2026, 2, 9, 15).toISOString()
    const nextDay = new Date(2026, 2, 10, 9).toISOString()

    const grouped = groupByLocalDay([
      { startUtc: morning },
      { startUtc: afternoon },
      { startUtc: nextDay },
    ])

    expect(grouped.get(localDayKey(new Date(2026, 2, 9)))).toHaveLength(2)
    expect(grouped.get(localDayKey(new Date(2026, 2, 10)))).toHaveLength(1)
  })

  it('повторенията се групират по своята дата, не по базовата', () => {
    const base = new Date(2026, 2, 2, 10).toISOString()
    const occurrence = new Date(2026, 2, 16, 10).toISOString()

    const grouped = groupByLocalDay([{ startUtc: base, occurrenceStartUtc: occurrence }])

    expect(grouped.get(localDayKey(new Date(2026, 2, 16)))).toHaveLength(1)
    expect(grouped.has(localDayKey(new Date(2026, 2, 2)))).toBe(false)
  })

  it('пропуска невалидни дати, вместо да пука', () => {
    expect(groupByLocalDay([{ startUtc: 'боклук' }]).size).toBe(0)
  })
})
