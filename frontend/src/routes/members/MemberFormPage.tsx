import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Select } from '@/components/ui/Select'
import { Spinner } from '@/components/ui/Spinner'
import {
  createMember,
  createMemberSchema,
  getMember,
  updateMember,
  updateMemberSchema,
  type CreateMemberForm,
} from '@/lib/api/members'
import { ApiError, errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { useDepartmentIdByCode, useDepartments } from '@/lib/hooks/useDepartments'
import { isOrgRole, SYSTEM_ROLES, SYSTEM_ROLE_LABELS } from '@/lib/types/enums'

const ROLE_OPTIONS = SYSTEM_ROLES.map((role) => ({ value: role, label: SYSTEM_ROLE_LABELS[role] }))

/** Today as `YYYY-MM-DD`, the format `<input type="date">` and the API both use. */
function today(): string {
  return new Date().toISOString().slice(0, 10)
}

/**
 * Create and edit share this screen: the fields are identical apart from the email, which is
 * only settable at creation.
 */
export function MemberFormPage() {
  const { id } = useParams()
  const isEdit = id !== undefined
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: departments, isPending: departmentsPending } = useDepartments()
  const departmentIdByCode = useDepartmentIdByCode()

  const { data: member, isPending: memberPending } = useQuery({
    queryKey: queryKeys.members.detail(id ?? ''),
    queryFn: () => getMember(id!),
    enabled: isEdit,
  })

  const {
    register,
    handleSubmit,
    watch,
    reset,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<CreateMemberForm>({
    resolver: zodResolver(isEdit ? updateMemberSchema : createMemberSchema),
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '',
      role: 'Member',
      departmentId: '',
      joinedOn: today(),
    },
  })

  const role = watch('role')
  const roleIsOrg = isOrgRole(role)

  // `GET /members/{id}` returns the department as a code, but `PUT` expects a Guid — the
  // department list is the only thing carrying both, so the form waits for it before filling in.
  useEffect(() => {
    if (!member || !departmentIdByCode) return

    reset({
      fullName: member.fullName,
      email: member.email,
      phoneNumber: member.phoneNumber ?? '',
      role: member.role,
      departmentId: member.department ? (departmentIdByCode[member.department] ?? '') : '',
      joinedOn: member.joinedOn,
    })
  }, [member, departmentIdByCode, reset])

  // An org role cannot carry a department; clear it so a stale value is never submitted.
  useEffect(() => {
    if (roleIsOrg) setValue('departmentId', '', { shouldValidate: false })
  }, [roleIsOrg, setValue])

  const save = useMutation({
    mutationFn: (values: CreateMemberForm) =>
      isEdit ? updateMember(id!, values) : createMember(values),
    onSuccess: async (saved) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.members.all })
      navigate(`/members/${saved.id}`, { replace: true })
    },
    onError: (error) => {
      // Server-side validation (duplicate email, unknown department) comes back per field.
      if (error instanceof ApiError && error.isValidation) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof CreateMemberForm
          if (messages[0]) setError(key, { message: messages[0] })
        }
      }
    },
  })

  if ((isEdit && memberPending) || departmentsPending) return <Spinner />

  const departmentOptions = (departments ?? []).map((department) => ({
    value: department.id,
    label: department.name,
  }))

  return (
    <div className="space-y-5">
      <Link
        to={isEdit ? `/members/${id}` : '/members'}
        className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Назад
      </Link>

      <h1 className="text-2xl font-semibold text-slate-900">
        {isEdit ? 'Редакция на член' : 'Нов член'}
      </h1>

      <form
        onSubmit={handleSubmit((values) => save.mutate(values))}
        noValidate
        className="max-w-xl space-y-4 rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200"
      >
        {save.isError && !(save.error instanceof ApiError && save.error.isValidation) && (
          <Alert tone="error">{errorMessage(save.error)}</Alert>
        )}

        <Input label="Име и фамилия" autoFocus error={errors.fullName?.message} {...register('fullName')} />

        {!isEdit && (
          <Input
            label="Имейл"
            type="email"
            hint="Временна парола се изпраща на този адрес."
            error={errors.email?.message}
            {...register('email')}
          />
        )}

        <Input
          label="Телефон"
          type="tel"
          error={errors.phoneNumber?.message}
          {...register('phoneNumber')}
        />

        <Select label="Роля" options={ROLE_OPTIONS} error={errors.role?.message} {...register('role')} />

        <Select
          label="Отдел"
          options={departmentOptions}
          placeholder={roleIsOrg ? 'Няма (организационно ниво)' : 'Избери отдел'}
          disabled={roleIsOrg}
          hint={
            roleIsOrg ? 'Организационните роли не се числят към отдел.' : undefined
          }
          error={errors.departmentId?.message}
          {...register('departmentId')}
        />

        <Input
          label="Присъединен на"
          type="date"
          error={errors.joinedOn?.message}
          {...register('joinedOn')}
        />

        <div className="flex justify-end gap-2 pt-2">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate(isEdit ? `/members/${id}` : '/members')}
          >
            Отказ
          </Button>
          <Button type="submit" loading={isSubmitting || save.isPending}>
            {isEdit ? 'Запази' : 'Създай член'}
          </Button>
        </div>
      </form>
    </div>
  )
}
