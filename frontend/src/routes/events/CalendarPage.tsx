import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { CalendarDays, ChevronLeft, ChevronRight, Download, Plus } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Select } from '@/components/ui/Select'
import { Spinner } from '@/components/ui/Spinner'
import { getEvents, icsExportPath, type EventFilters } from '@/lib/api/events'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { canCreateEvent, toEventActor } from '@/lib/auth/eventPermissions'
import { useAuth } from '@/lib/auth/useAuth'
import {
  DEPARTMENT_CODES,
  DEPARTMENT_CODE_LABELS,
  EVENT_TYPES,
  type CalendarView,
} from '@/lib/types/enums'
import { buildMonthGrid, dayWindow, gridWindow, monthLabel, weekWindow } from '@/lib/utils/calendarGrid'
import { cn } from '@/lib/utils/cn'
import { downloadAuthenticatedFile } from '@/lib/utils/download'

import { EventAgenda } from './EventAgenda'
import { EVENT_TYPE_LABELS } from './eventLabels'
import { MonthGrid } from './MonthGrid'

const VIEW_LABELS: Record<CalendarView, string> = {
  month: 'Месец',
  week: 'Седмица',
  day: 'Ден',
  list: 'Списък',
}

const DAY_HEADING = new Intl.DateTimeFormat('bg-BG', { day: 'numeric', month: 'long', year: 'numeric' })

export function CalendarPage() {
  const navigate = useNavigate()
  const { user } = useAuth()

  const [view, setView] = useState<CalendarView>('month')
  /** Anchor for the visible period; navigation moves it, the view decides the window. */
  const [anchor, setAnchor] = useState(() => new Date())
  const [type, setType] = useState<EventFilters['type']>('')
  const [department, setDepartment] = useState<EventFilters['department']>('')
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const weeks = useMemo(
    () => buildMonthGrid(anchor.getFullYear(), anchor.getMonth()),
    [anchor],
  )

  // Explicit bounds beat `view` server-side, so the client owns which period is shown and the
  // API only has to resolve the "list" look-ahead.
  const window = useMemo(() => {
    if (view === 'month') return gridWindow(weeks)
    if (view === 'week') return weekWindow(anchor)
    if (view === 'day') return dayWindow(anchor)
    return {}
  }, [view, weeks, anchor])

  const filters: EventFilters = { ...window, type, department, view }

  const { data: events, isPending, isError, error } = useQuery({
    queryKey: queryKeys.events.list(filters),
    queryFn: () => getEvents(filters),
  })

  const step = (direction: -1 | 1) => {
    setAnchor((current) => {
      const next = new Date(current)
      if (view === 'month') next.setMonth(next.getMonth() + direction)
      else if (view === 'week') next.setDate(next.getDate() + 7 * direction)
      else next.setDate(next.getDate() + direction)
      return next
    })
  }

  const handleExport = async () => {
    setDownloadError(null)
    try {
      await downloadAuthenticatedFile(icsExportPath(filters), 'studentcouncil.ics')
    } catch (cause) {
      setDownloadError(errorMessage(cause))
    }
  }

  const actor = user ? toEventActor(user) : null

  const periodLabel =
    view === 'month'
      ? monthLabel(anchor.getFullYear(), anchor.getMonth())
      : view === 'list'
        ? 'Следващите 90 дни'
        : DAY_HEADING.format(anchor)

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Календар</h1>

        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => void handleExport()}>
            <Download aria-hidden className="size-4" />
            Износ .ics
          </Button>

          {actor && canCreateEvent(actor) && (
            <Button onClick={() => navigate('/events/new')}>
              <Plus aria-hidden className="size-4" />
              Ново събитие
            </Button>
          )}
        </div>
      </div>

      {downloadError && <Alert tone="error">{downloadError}</Alert>}

      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex items-center gap-2">
          {/* The list view is a rolling 90-day look-ahead, so stepping through it is meaningless. */}
          {view !== 'list' && (
            <>
              <Button variant="secondary" size="sm" onClick={() => step(-1)} aria-label="Предишен период">
                <ChevronLeft aria-hidden className="size-4" />
              </Button>
              <Button variant="secondary" size="sm" onClick={() => step(1)} aria-label="Следващ период">
                <ChevronRight aria-hidden className="size-4" />
              </Button>
              <Button variant="ghost" size="sm" onClick={() => setAnchor(new Date())}>
                Днес
              </Button>
            </>
          )}
          <span className="ml-1 text-sm font-medium text-ink first-letter:uppercase">
            {periodLabel}
          </span>
        </div>

        <div role="tablist" aria-label="Изглед" className="flex gap-1 rounded-lg bg-page p-1">
          {(Object.keys(VIEW_LABELS) as CalendarView[]).map((candidate) => (
            <button
              key={candidate}
              type="button"
              role="tab"
              aria-selected={view === candidate}
              onClick={() => setView(candidate)}
              className={cn(
                'rounded-md px-3 py-1 text-sm font-medium transition-colors',
                view === candidate ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink',
              )}
            >
              {VIEW_LABELS[candidate]}
            </button>
          ))}
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:w-1/2">
        <Select
          label="Вид"
          placeholder="Всички"
          options={EVENT_TYPES.map((value) => ({ value, label: EVENT_TYPE_LABELS[value] }))}
          value={type ?? ''}
          onChange={(event) => setType(event.target.value as EventFilters['type'])}
        />
        <Select
          label="Отдел"
          placeholder="Всички"
          options={DEPARTMENT_CODES.map((value) => ({ value, label: DEPARTMENT_CODE_LABELS[value] }))}
          value={department ?? ''}
          onChange={(event) => setDepartment(event.target.value as EventFilters['department'])}
        />
      </div>

      {isError ? (
        <Alert tone="error">{errorMessage(error)}</Alert>
      ) : isPending ? (
        <Spinner />
      ) : view === 'month' ? (
        <MonthGrid weeks={weeks} events={events} />
      ) : (
        <EventAgenda events={events} />
      )}

      <p className="flex items-center gap-1.5 text-xs text-muted">
        <CalendarDays aria-hidden className="size-3.5" />
        Крайните срокове на задачи се показват тук автоматично и се редактират през самата задача.
      </p>
    </div>
  )
}
