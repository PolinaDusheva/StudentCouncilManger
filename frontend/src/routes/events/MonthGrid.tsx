import { Link } from 'react-router-dom'

import type { EventDto } from '@/lib/types/dto'
import { cn } from '@/lib/utils/cn'
import { groupByLocalDay, localDayKey, WEEKDAY_LABELS, type GridDay } from '@/lib/utils/calendarGrid'

import { EVENT_TYPE_DOTS } from './eventLabels'

interface MonthGridProps {
  weeks: GridDay[][]
  events: EventDto[]
}

/** Time of day in the browser's local zone, e.g. `14:30`. */
const TIME = new Intl.DateTimeFormat('bg-BG', { hour: '2-digit', minute: '2-digit' })

/** Month calendar: one cell per day, each listing the entries that start on it. */
export function MonthGrid({ weeks, events }: MonthGridProps) {
  const byDay = groupByLocalDay(events)

  return (
    <div className="overflow-hidden rounded-xl bg-white ring-1 ring-slate-200">
      <div className="grid grid-cols-7 border-b border-slate-200 bg-slate-50">
        {WEEKDAY_LABELS.map((label) => (
          <div key={label} className="px-2 py-2 text-center text-xs font-medium text-slate-500">
            {label}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7">
        {weeks.flat().map((day) => (
          <DayCell key={day.date.toISOString()} day={day} entries={byDay.get(localDayKey(day.date)) ?? []} />
        ))}
      </div>
    </div>
  )
}

function DayCell({ day, entries }: { day: GridDay; entries: EventDto[] }) {
  // Three fit without the cell growing; the rest are summarised.
  const visible = entries.slice(0, 3)
  const hidden = entries.length - visible.length

  return (
    <div
      className={cn(
        'min-h-24 border-r border-b border-slate-100 p-1.5',
        !day.inMonth && 'bg-slate-50/60',
      )}
    >
      <span
        className={cn(
          'inline-flex size-6 items-center justify-center rounded-full text-xs',
          day.isToday && 'bg-brand-600 font-semibold text-white',
          !day.isToday && day.inMonth && 'text-slate-700',
          !day.isToday && !day.inMonth && 'text-slate-400',
        )}
      >
        {day.date.getDate()}
      </span>

      <ul className="mt-1 space-y-0.5">
        {visible.map((entry) => (
          <li key={`${entry.id}-${entry.occurrenceStartUtc ?? entry.startUtc}`}>
            <EntryLine entry={entry} />
          </li>
        ))}
      </ul>

      {hidden > 0 && <p className="mt-0.5 px-1 text-xs text-slate-500">+{hidden} още</p>}
    </div>
  )
}

function EntryLine({ entry }: { entry: EventDto }) {
  const start = new Date(entry.occurrenceStartUtc ?? entry.startUtc)

  // A deadline is a projection of a task, so it links to the task rather than an event page.
  const to = entry.isDeadline && entry.taskId ? `/tasks/${entry.taskId}` : `/events/${entry.id}`

  return (
    <Link
      to={to}
      title={entry.title}
      className="flex items-center gap-1 rounded px-1 py-0.5 text-xs hover:bg-slate-100"
    >
      <span aria-hidden className={cn('size-1.5 shrink-0 rounded-full', EVENT_TYPE_DOTS[entry.type])} />
      <span className="shrink-0 text-slate-500">{TIME.format(start)}</span>
      <span className="truncate text-slate-900">{entry.title}</span>
    </Link>
  )
}
