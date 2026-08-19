import { Link } from 'react-router-dom'
import { MapPin, Repeat } from 'lucide-react'

import { Badge } from '@/components/ui/Badge'
import { EmptyState } from '@/components/ui/EmptyState'
import type { EventDto } from '@/lib/types/dto'
import { DEPARTMENT_CODE_LABELS } from '@/lib/types/enums'
import { groupByLocalDay } from '@/lib/utils/calendarGrid'

import { EVENT_TYPE_LABELS, EVENT_TYPE_TONES } from './eventLabels'

const DAY_HEADING = new Intl.DateTimeFormat('bg-BG', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
})
const TIME = new Intl.DateTimeFormat('bg-BG', { hour: '2-digit', minute: '2-digit' })

/**
 * Chronological list grouped by day. Used for the day, week and list windows, where a grid
 * would add nothing over a plain agenda.
 */
export function EventAgenda({ events }: { events: EventDto[] }) {
  const byDay = groupByLocalDay(events)

  if (byDay.size === 0) {
    return (
      <div className="rounded-[15px] bg-surface ring-1 ring-divider">
        <EmptyState title="Няма събития в този период" />
      </div>
    )
  }

  // The API returns entries in order, but recurring occurrences are appended per base event,
  // so the day keys have to be sorted explicitly.
  const days = [...byDay.entries()].sort(([a], [b]) => a.localeCompare(b))

  return (
    <div className="space-y-5">
      {days.map(([dayKey, entries]) => (
        <section key={dayKey}>
          <h2 className="mb-2 text-sm font-semibold text-ink first-letter:uppercase">
            {DAY_HEADING.format(new Date(`${dayKey}T12:00:00`))}
          </h2>

          <ul className="divide-y divide-divider overflow-hidden rounded-[15px] bg-surface ring-1 ring-divider">
            {entries
              .slice()
              .sort(
                (a, b) =>
                  new Date(a.occurrenceStartUtc ?? a.startUtc).getTime() -
                  new Date(b.occurrenceStartUtc ?? b.startUtc).getTime(),
              )
              .map((entry) => (
                <li key={`${entry.id}-${entry.occurrenceStartUtc ?? entry.startUtc}`}>
                  <AgendaRow entry={entry} />
                </li>
              ))}
          </ul>
        </section>
      ))}
    </div>
  )
}

function AgendaRow({ entry }: { entry: EventDto }) {
  const start = new Date(entry.occurrenceStartUtc ?? entry.startUtc)
  const end = new Date(entry.endUtc)
  const to = entry.isDeadline && entry.taskId ? `/tasks/${entry.taskId}` : `/events/${entry.id}`

  return (
    <Link to={to} className="flex gap-4 px-4 py-3 hover:bg-row-hover">
      <span className="w-24 shrink-0 text-sm text-muted">
        {TIME.format(start)} – {TIME.format(end)}
      </span>

      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium text-ink">{entry.title}</span>

        <span className="mt-1 flex flex-wrap items-center gap-2 text-xs text-muted">
          <Badge tone={EVENT_TYPE_TONES[entry.type]}>{EVENT_TYPE_LABELS[entry.type]}</Badge>

          {entry.department && <span>{DEPARTMENT_CODE_LABELS[entry.department]}</span>}

          {entry.location && (
            <span className="inline-flex items-center gap-1">
              <MapPin aria-hidden className="size-3.5" />
              {entry.location}
            </span>
          )}

          {entry.recurrence !== 'None' && (
            <span className="inline-flex items-center gap-1">
              <Repeat aria-hidden className="size-3.5" />
              повтарящо се
            </span>
          )}

          {entry.isDeadline && <span className="text-danger">краен срок на задача</span>}
        </span>
      </span>
    </Link>
  )
}
