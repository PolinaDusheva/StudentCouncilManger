/**
 * Display formatting for the dates the API returns.
 *
 * `DateOnly` fields arrive as `YYYY-MM-DD` and `DateTime` fields as ISO-8601 in UTC.
 * Times are rendered in the browser's local zone; plain dates are rendered as-is so the
 * day does not shift for viewers west of Greenwich.
 */

const EMPTY = '—'

const DATE_ONLY_PATTERN = /^\d{4}-\d{2}-\d{2}$/

const DATE = new Intl.DateTimeFormat('bg-BG', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

const DATE_TIME = new Intl.DateTimeFormat('bg-BG', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

function parse(value: string | null | undefined): Date | null {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatDate(value: string | null | undefined): string {
  const date = parse(value)
  if (!date) return EMPTY

  // `Date` reads a bare `YYYY-MM-DD` as midnight UTC. Formatted in a zone behind UTC that
  // renders as the previous day, so shift it back to the wall-clock date the API meant.
  if (DATE_ONLY_PATTERN.test(value!)) {
    return DATE.format(new Date(date.getTime() + date.getTimezoneOffset() * 60_000))
  }

  return DATE.format(date)
}

export function formatDateTime(value: string | null | undefined): string {
  const date = parse(value)
  return date ? DATE_TIME.format(date) : EMPTY
}

const EUR = new Intl.NumberFormat('bg-BG', {
  style: 'currency',
  currency: 'EUR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

/**
 * Money as stored by the API: `decimal(10,2)` in euro. Always shows two decimals, because a
 * budget figure that renders as `12 €` next to `12,50 €` reads as a different precision.
 */
export function formatEur(value: number | null | undefined): string {
  return value === null || value === undefined || Number.isNaN(value) ? EMPTY : EUR.format(value)
}
