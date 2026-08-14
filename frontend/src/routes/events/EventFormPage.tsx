import { useEffect, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { MemberPicker } from '@/components/ui/MemberPicker'
import { Select } from '@/components/ui/Select'
import { Spinner } from '@/components/ui/Spinner'
import {
  createEvent,
  eventFormSchema,
  getEvent,
  updateEvent,
  type EventForm,
} from '@/lib/api/events'
import { ApiError, errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { toLocalInputValue } from '@/lib/api/tasks'
import { useDepartmentIdByCode, useDepartments } from '@/lib/hooks/useDepartments'
import type { EventConflictDto } from '@/lib/types/dto'
import { EVENT_TYPES, RECURRENCE_TYPES } from '@/lib/types/enums'
import { formatDateTime } from '@/lib/utils/format'

import { EVENT_TYPE_LABELS, RECURRENCE_LABELS } from './eventLabels'

const TYPE_OPTIONS = EVENT_TYPES.map((value) => ({ value, label: EVENT_TYPE_LABELS[value] }))
const RECURRENCE_OPTIONS = RECURRENCE_TYPES.map((value) => ({
  value,
  label: RECURRENCE_LABELS[value],
}))

export function EventFormPage() {
  const { id } = useParams()
  const isEdit = id !== undefined
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: departments, isPending: departmentsPending } = useDepartments()
  const departmentIdByCode = useDepartmentIdByCode()

  /** Overlaps reported by the server. The event is already saved — this is only a warning. */
  const [conflicts, setConflicts] = useState<EventConflictDto[]>([])

  const { data: event, isPending: eventPending } = useQuery({
    queryKey: queryKeys.events.detail(id ?? ''),
    queryFn: () => getEvent(id!),
    enabled: isEdit,
  })

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<EventForm>({
    resolver: zodResolver(eventFormSchema),
    defaultValues: {
      title: '',
      description: '',
      location: '',
      type: 'Meeting',
      recurrence: 'None',
      startLocal: '',
      endLocal: '',
      departmentId: '',
      participantIds: [],
    },
  })

  // `GET /events/{id}` returns the department as a code; the form needs the Guid.
  useEffect(() => {
    if (!event || !departmentIdByCode) return

    reset({
      title: event.title,
      description: event.description ?? '',
      location: event.location ?? '',
      type: event.type,
      recurrence: event.recurrence,
      startLocal: toLocalInputValue(event.startUtc),
      endLocal: toLocalInputValue(event.endUtc),
      departmentId: event.department ? (departmentIdByCode[event.department] ?? '') : '',
      participantIds: event.participants.map((participant) => participant.id),
    })
  }, [event, departmentIdByCode, reset])

  const save = useMutation({
    mutationFn: (values: EventForm) => (isEdit ? updateEvent(id!, values) : createEvent(values)),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.events.all })

      // Overlaps do not block the save, so the member is shown the warning here instead of
      // being navigated away from it.
      if (result.conflictsWith.length > 0) {
        setConflicts(result.conflictsWith)
        return
      }

      navigate(`/events/${result.event.id}`, { replace: true })
    },
    onError: (error) => {
      if (error instanceof ApiError && error.isValidation) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof EventForm
          if (messages[0]) setError(key, { message: messages[0] })
        }
      }
    },
  })

  if ((isEdit && eventPending) || departmentsPending) return <Spinner />

  const departmentOptions = (departments ?? []).map((department) => ({
    value: department.id,
    label: department.name,
  }))

  if (conflicts.length > 0 && save.data) {
    const savedId = save.data.event.id

    return (
      <div className="max-w-2xl space-y-4">
        <Alert tone="warning">
          <p className="font-medium">Събитието е запазено, но се застъпва с други.</p>
          <ul className="mt-2 list-disc space-y-1 pl-4">
            {conflicts.map((conflict) => (
              <li key={conflict.id}>
                <Link to={`/events/${conflict.id}`} className="underline">
                  {conflict.title}
                </Link>{' '}
                — {formatDateTime(conflict.startUtc)}
              </li>
            ))}
          </ul>
        </Alert>

        <div className="flex gap-2">
          <Button onClick={() => navigate(`/events/${savedId}`, { replace: true })}>
            Към събитието
          </Button>
          <Button variant="secondary" onClick={() => setConflicts([])}>
            Продължи редакцията
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-5">
      <Link
        to={isEdit ? `/events/${id}` : '/events'}
        className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Назад
      </Link>

      <h1 className="text-2xl font-semibold text-slate-900">
        {isEdit ? 'Редакция на събитие' : 'Ново събитие'}
      </h1>

      <form
        onSubmit={handleSubmit((values) => save.mutate(values))}
        noValidate
        className="max-w-2xl space-y-4 rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200"
      >
        {save.isError && !(save.error instanceof ApiError && save.error.isValidation) && (
          <Alert tone="error">{errorMessage(save.error)}</Alert>
        )}

        <Input label="Заглавие" autoFocus error={errors.title?.message} {...register('title')} />

        <div className="space-y-1.5">
          <label htmlFor="event-description" className="block text-sm font-medium text-slate-700">
            Описание
          </label>
          <textarea
            id="event-description"
            rows={3}
            className="focus:ring-brand-500 block w-full rounded-lg border-0 px-3 py-2 text-sm text-slate-900 shadow-sm ring-1 ring-slate-300 ring-inset focus:ring-2 focus:ring-inset focus:outline-none"
            {...register('description')}
          />
          {errors.description && (
            <p role="alert" className="text-sm text-red-600">
              {errors.description.message}
            </p>
          )}
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <Input
            label="Начало"
            type="datetime-local"
            error={errors.startLocal?.message}
            {...register('startLocal')}
          />
          <Input
            label="Край"
            type="datetime-local"
            error={errors.endLocal?.message}
            {...register('endLocal')}
          />
        </div>

        <Input label="Място" error={errors.location?.message} {...register('location')} />

        <div className="grid gap-4 sm:grid-cols-2">
          <Select label="Вид" options={TYPE_OPTIONS} error={errors.type?.message} {...register('type')} />
          <Select
            label="Повторение"
            options={RECURRENCE_OPTIONS}
            error={errors.recurrence?.message}
            {...register('recurrence')}
          />
        </div>

        <Select
          label="Отдел"
          options={departmentOptions}
          placeholder="Цялата организация"
          hint="Остави празно за събитие на цялата организация."
          error={errors.departmentId?.message}
          {...register('departmentId')}
        />

        <Controller
          control={control}
          name="participantIds"
          render={({ field }) => (
            <MemberPicker
              label="Участници"
              value={field.value}
              onChange={field.onChange}
              knownMembers={event?.participants}
              error={errors.participantIds?.message}
              hint="По избор."
            />
          )}
        />

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(isEdit ? `/events/${id}` : '/events')}
          >
            Отказ
          </Button>
          <Button type="submit" loading={isSubmitting || save.isPending}>
            {isEdit ? 'Запази' : 'Създай събитие'}
          </Button>
        </div>
      </form>
    </div>
  )
}
