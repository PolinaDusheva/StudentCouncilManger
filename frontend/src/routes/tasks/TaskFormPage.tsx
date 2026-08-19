import { useEffect } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { MemberPicker } from '@/components/ui/MemberPicker'
import { Select } from '@/components/ui/Select'
import { Spinner } from '@/components/ui/Spinner'
import { ApiError, errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import {
  createTask,
  createTaskSchema,
  getTask,
  toLocalInputValue,
  updateTask,
  updateTaskSchema,
  type CreateTaskForm,
} from '@/lib/api/tasks'
import { canCreateScope, toTaskActor } from '@/lib/auth/taskPermissions'
import { useAuth } from '@/lib/auth/useAuth'
import { useDepartmentIdByCode, useDepartments } from '@/lib/hooks/useDepartments'
import { DEPARTMENT_CODE_LABELS, TASK_PRIORITIES, TASK_SCOPES } from '@/lib/types/enums'

import { TASK_PRIORITY_LABELS, TASK_SCOPE_LABELS } from './taskLabels'

const PRIORITY_OPTIONS = TASK_PRIORITIES.map((priority) => ({
  value: priority,
  label: TASK_PRIORITY_LABELS[priority],
}))

export function TaskFormPage() {
  const { id } = useParams()
  const isEdit = id !== undefined
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()

  const { data: departments, isPending: departmentsPending } = useDepartments()
  const departmentIdByCode = useDepartmentIdByCode()

  const { data: task, isPending: taskPending } = useQuery({
    queryKey: queryKeys.tasks.detail(id ?? ''),
    queryFn: () => getTask(id!),
    enabled: isEdit,
  })

  const actor = user ? toTaskActor(user) : null

  // Only the scopes this member may actually create are offered; the server enforces the
  // same rule in CreateTask.EnsureScopeAllowed.
  const scopeOptions = TASK_SCOPES.filter((scope) => actor && canCreateScope(scope, actor)).map(
    (scope) => ({ value: scope, label: TASK_SCOPE_LABELS[scope] }),
  )

  const {
    register,
    control,
    handleSubmit,
    watch,
    reset,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateTaskForm>({
    resolver: zodResolver(isEdit ? updateTaskSchema : createTaskSchema),
    defaultValues: {
      title: '',
      description: '',
      priority: 'Medium',
      scope: scopeOptions[0]?.value ?? 'Departmental',
      departmentId: '',
      dueAtLocal: '',
      assigneeIds: [],
    },
  })

  const scope = watch('scope')
  const scopeIsOrganizational = scope === 'Organizational'

  // `GET /tasks/{id}` returns the department as a code; the form needs the Guid.
  useEffect(() => {
    if (!task || !departmentIdByCode) return

    reset({
      title: task.title,
      description: task.description ?? '',
      priority: task.priority,
      scope: task.scope,
      departmentId: task.department ? (departmentIdByCode[task.department] ?? '') : '',
      dueAtLocal: toLocalInputValue(task.dueAtUtc),
      assigneeIds: task.assignees.map((assignee) => assignee.id),
    })
  }, [task, departmentIdByCode, reset])

  // An organisational task must not carry a department; clear a stale selection.
  useEffect(() => {
    if (!isEdit && scopeIsOrganizational) setValue('departmentId', '', { shouldValidate: false })
  }, [isEdit, scopeIsOrganizational, setValue])

  const save = useMutation({
    mutationFn: (values: CreateTaskForm) =>
      isEdit ? updateTask(id!, values) : createTask(values),
    onSuccess: async (saved) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.all })
      navigate(`/tasks/${saved.id}`, { replace: true })
    },
    onError: (error) => {
      if (error instanceof ApiError && error.isValidation) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof CreateTaskForm
          if (messages[0]) setError(key, { message: messages[0] })
        }
      }
    },
  })

  if ((isEdit && taskPending) || departmentsPending) return <Spinner />

  const departmentOptions = (departments ?? []).map((department) => ({
    value: department.id,
    label: department.name,
  }))

  return (
    <div className="space-y-5">
      <Link
        to={isEdit ? `/tasks/${id}` : '/tasks'}
        className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Назад
      </Link>

      <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">
        {isEdit ? 'Редакция на задача' : 'Нова задача'}
      </h1>

      <form
        onSubmit={handleSubmit((values) => save.mutate(values))}
        noValidate
        className="max-w-2xl space-y-4 rounded-[20px] bg-surface p-6 shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider"
      >
        {save.isError && !(save.error instanceof ApiError && save.error.isValidation) && (
          <Alert tone="error">{errorMessage(save.error)}</Alert>
        )}

        <Input label="Заглавие" autoFocus error={errors.title?.message} {...register('title')} />

        <div className="space-y-1.5">
          <label htmlFor="task-description" className="block text-sm font-semibold text-ink-soft">
            Описание
          </label>
          <textarea
            id="task-description"
            rows={4}
            className="block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none"
            {...register('description')}
          />
          {errors.description && (
            <p role="alert" className="text-sm text-danger">
              {errors.description.message}
            </p>
          )}
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <Select
            label="Приоритет"
            options={PRIORITY_OPTIONS}
            error={errors.priority?.message}
            {...register('priority')}
          />

          <Input
            label="Краен срок"
            type="datetime-local"
            hint="По избор. Трябва да е в бъдещето."
            error={errors.dueAtLocal?.message}
            {...register('dueAtLocal')}
          />
        </div>

        {isEdit ? (
          // Scope and department are fixed at creation — PUT /tasks/{id} does not accept them.
          <div className="space-y-1.5">
            <span className="block text-sm font-semibold text-ink-soft">Вид</span>
            <div className="flex flex-wrap gap-2">
              <Badge>{TASK_SCOPE_LABELS[task!.scope]}</Badge>
              {task!.department && <Badge>{DEPARTMENT_CODE_LABELS[task!.department]}</Badge>}
            </div>
            <p className="text-sm text-muted">
              Видът и отделът се задават при създаване и не се променят.
            </p>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            <Select
              label="Вид"
              options={scopeOptions}
              error={errors.scope?.message}
              {...register('scope')}
            />

            <Select
              label="Отдел"
              options={departmentOptions}
              placeholder={scopeIsOrganizational ? 'Няма (организационна)' : 'Избери отдел'}
              disabled={scopeIsOrganizational}
              hint={
                scopeIsOrganizational
                  ? 'Организационните задачи не се числят към отдел.'
                  : undefined
              }
              error={errors.departmentId?.message}
              {...register('departmentId')}
            />
          </div>
        )}

        <Controller
          control={control}
          name="assigneeIds"
          render={({ field }) => (
            <MemberPicker
              label="Изпълнители"
              value={field.value}
              onChange={field.onChange}
              knownMembers={task?.assignees}
              error={errors.assigneeIds?.message}
              hint="Поне един. Само активни членове."
            />
          )}
        />

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(isEdit ? `/tasks/${id}` : '/tasks')}
          >
            Отказ
          </Button>
          <Button type="submit" loading={isSubmitting || save.isPending}>
            {isEdit ? 'Запази' : 'Създай задача'}
          </Button>
        </div>
      </form>
    </div>
  )
}
