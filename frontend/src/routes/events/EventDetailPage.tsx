import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, MapPin, Pencil, Repeat, Trash2 } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Spinner } from '@/components/ui/Spinner'
import { deleteEvent, getEvent } from '@/lib/api/events'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { canDeleteEvent, canEditEvent, toEventActor } from '@/lib/auth/eventPermissions'
import { useAuth } from '@/lib/auth/useAuth'
import type { MemberSummaryDto } from '@/lib/types/dto'
import { DEPARTMENT_CODE_LABELS } from '@/lib/types/enums'
import { formatDateTime } from '@/lib/utils/format'

import { EVENT_TYPE_LABELS, EVENT_TYPE_TONES, RECURRENCE_LABELS } from './eventLabels'

export function EventDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [confirmOpen, setConfirmOpen] = useState(false)

  const { data: event, isPending, isError, error } = useQuery({
    queryKey: queryKeys.events.detail(id),
    queryFn: () => getEvent(id),
    enabled: id !== '',
  })

  const remove = useMutation({
    mutationFn: () => deleteEvent(id),
    onSuccess: async () => {
      setConfirmOpen(false)
      await queryClient.invalidateQueries({ queryKey: queryKeys.events.all })
      navigate('/events', { replace: true })
    },
  })

  if (isPending) return <Spinner />

  if (isError) {
    return (
      <div className="space-y-4">
        <BackLink />
        <Alert tone="error">{errorMessage(error)}</Alert>
      </div>
    )
  }

  const actor = user ? toEventActor(user) : null
  const canEdit = actor ? canEditEvent(event, actor) : false
  const canDelete = actor ? canDeleteEvent(event, actor) : false

  return (
    <div className="space-y-5">
      <BackLink />

      <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <h1 className="text-xl font-semibold text-slate-900">{event.title}</h1>

          <div className="flex gap-2">
            {canEdit && (
              <Button variant="secondary" onClick={() => navigate(`/events/${event.id}/edit`)}>
                <Pencil aria-hidden className="size-4" />
                Редактирай
              </Button>
            )}
            {canDelete && (
              <Button variant="danger" onClick={() => setConfirmOpen(true)}>
                <Trash2 aria-hidden className="size-4" />
                Изтрий
              </Button>
            )}
          </div>
        </div>

        <div className="mt-3 flex flex-wrap gap-2">
          <Badge tone={EVENT_TYPE_TONES[event.type]}>{EVENT_TYPE_LABELS[event.type]}</Badge>
          {event.department && <Badge>{DEPARTMENT_CODE_LABELS[event.department]}</Badge>}
          {event.recurrence !== 'None' && (
            <Badge tone="info">
              <Repeat aria-hidden className="mr-1 inline size-3" />
              {RECURRENCE_LABELS[event.recurrence]}
            </Badge>
          )}
        </div>

        {event.description && (
          <p className="mt-5 border-t border-slate-200 pt-5 text-sm whitespace-pre-wrap text-slate-700">
            {event.description}
          </p>
        )}

        <dl className="mt-5 grid gap-x-6 gap-y-4 border-t border-slate-200 pt-5 text-sm sm:grid-cols-2">
          <Field label="Начало" value={formatDateTime(event.startUtc)} />
          <Field label="Край" value={formatDateTime(event.endUtc)} />
          {event.location && (
            <div>
              <dt className="text-slate-500">Място</dt>
              <dd className="mt-0.5 flex items-center gap-1.5 font-medium text-slate-900">
                <MapPin aria-hidden className="size-4 text-slate-400" />
                {event.location}
              </dd>
            </div>
          )}
          {event.organizer && <Field label="Организатор" value={event.organizer.fullName} />}
        </dl>
      </div>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">
          Участници ({event.participants.length})
        </h2>

        {event.participants.length === 0 ? (
          <p className="text-sm text-slate-500">Няма записани участници.</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {event.participants.map((participant) => (
              <li key={participant.id}>
                <MemberLine member={participant} />
              </li>
            ))}
          </ul>
        )}
      </section>

      {remove.isError && <Alert tone="error">{errorMessage(remove.error)}</Alert>}

      <ConfirmDialog
        open={confirmOpen}
        title="Изтриване на събитие"
        message={`„${event.title}“ ще бъде изтрито завинаги.${
          event.recurrence !== 'None' ? ' Изтриват се всички повторения.' : ''
        }`}
        confirmLabel="Изтрий"
        loading={remove.isPending}
        onConfirm={() => remove.mutate()}
        onCancel={() => setConfirmOpen(false)}
      />
    </div>
  )
}

function BackLink() {
  return (
    <Link
      to="/events"
      className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
    >
      <ArrowLeft aria-hidden className="size-4" />
      Календар
    </Link>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{value}</dd>
    </div>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-brand-700 truncate text-sm font-medium text-slate-900 group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
