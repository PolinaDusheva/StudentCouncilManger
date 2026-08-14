/**
 * Pure date maths for the month calendar.
 *
 * Weeks start on **Monday**, matching `CalendarWindow.WeekWindow` on the server and the
 * Bulgarian convention. All functions work in the browser's local zone: the grid is what the
 * user sees on their wall, while the API speaks UTC — {@link gridWindow} bridges the two.
 */

export interface GridDay {
  /** Local midnight of this cell. */
  date: Date
  /** False for the leading/trailing days borrowed from the adjacent months. */
  inMonth: boolean
  isToday: boolean
}

const MS_PER_DAY = 24 * 60 * 60 * 1000

/** Monday-based weekday index: Mon → 0 … Sun → 6. */
export function mondayIndex(date: Date): number {
  return (date.getDay() + 6) % 7
}

export function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

export function isSameDay(a: Date, b: Date): boolean {
  return (
    a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
  )
}

/**
 * Six weeks of 42 cells covering `month`, padded with the surrounding days so every row is
 * full. A fixed height keeps the grid from jumping between months.
 */
export function buildMonthGrid(year: number, month: number, today = new Date()): GridDay[][] {
  const firstOfMonth = new Date(year, month, 1)
  const gridStart = new Date(year, month, 1 - mondayIndex(firstOfMonth))

  const weeks: GridDay[][] = []
  for (let week = 0; week < 6; week += 1) {
    const days: GridDay[] = []
    for (let day = 0; day < 7; day += 1) {
      const date = new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + week * 7 + day)
      days.push({ date, inMonth: date.getMonth() === month, isToday: isSameDay(date, today) })
    }
    weeks.push(days)
  }

  return weeks
}

/**
 * The `[from, to)` window to request for a rendered grid, as ISO-8601 UTC.
 *
 * Covers the whole grid rather than just the month, so events land on the leading and
 * trailing cells too instead of appearing blank.
 */
export function gridWindow(weeks: GridDay[][]): { from: string; to: string } {
  const first = weeks[0]?.[0]?.date ?? new Date()
  const lastWeek = weeks.at(-1)
  const last = lastWeek?.at(-1)?.date ?? new Date()

  return {
    from: first.toISOString(),
    // Exclusive upper bound: the day after the final cell.
    to: new Date(last.getTime() + MS_PER_DAY).toISOString(),
  }
}

/** The `[from, to)` window for a single local day. */
export function dayWindow(date: Date): { from: string; to: string } {
  const start = startOfDay(date)
  return { from: start.toISOString(), to: new Date(start.getTime() + MS_PER_DAY).toISOString() }
}

/** The `[from, to)` window for the Monday-based week containing `date`. */
export function weekWindow(date: Date): { from: string; to: string } {
  const start = startOfDay(date)
  const monday = new Date(start.getFullYear(), start.getMonth(), start.getDate() - mondayIndex(start))
  return { from: monday.toISOString(), to: new Date(monday.getTime() + 7 * MS_PER_DAY).toISOString() }
}

/** Month names in Bulgarian, in the nominative used for a calendar heading. */
const MONTH_NAMES = [
  'Януари',
  'Февруари',
  'Март',
  'Април',
  'Май',
  'Юни',
  'Юли',
  'Август',
  'Септември',
  'Октомври',
  'Ноември',
  'Декември',
]

export function monthLabel(year: number, month: number): string {
  return `${MONTH_NAMES[month]} ${year}`
}

/** Monday-first weekday abbreviations. */
export const WEEKDAY_LABELS = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'нд']

/**
 * Groups entries by the local day they start on, keyed by `YYYY-MM-DD`.
 * Recurring occurrences carry `occurrenceStartUtc`; it wins over the base `startUtc`.
 */
export function groupByLocalDay<T extends { startUtc: string; occurrenceStartUtc?: string | null }>(
  entries: T[],
): Map<string, T[]> {
  const byDay = new Map<string, T[]>()

  for (const entry of entries) {
    const start = new Date(entry.occurrenceStartUtc ?? entry.startUtc)
    if (Number.isNaN(start.getTime())) continue

    const key = localDayKey(start)
    const existing = byDay.get(key)
    if (existing) existing.push(entry)
    else byDay.set(key, [entry])
  }

  return byDay
}

/** `YYYY-MM-DD` in local time — the key used by {@link groupByLocalDay}. */
export function localDayKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}
