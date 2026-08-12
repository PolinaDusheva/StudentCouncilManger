import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { zodResolver } from '@hookform/resolvers/zod'
import { Upload } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Spinner } from '@/components/ui/Spinner'
import {
  getMyProfile,
  updateMyPhoto,
  updateMyProfile,
  updateMyProfileSchema,
  type UpdateMyProfileForm,
} from '@/lib/api/members'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { useAuth } from '@/lib/auth/useAuth'
import { formatDate } from '@/lib/utils/format'
import {
  DEPARTMENT_CODE_LABELS,
  IMAGE_CONTENT_TYPES,
  SYSTEM_ROLE_LABELS,
} from '@/lib/types/enums'

import { roleTone } from './memberLabels'

/** Server-side cap for avatars (`MaxAvatarSizeMb`); checked here to fail before the upload. */
const MAX_PHOTO_BYTES = 5 * 1024 * 1024

export function MyProfilePage() {
  const queryClient = useQueryClient()
  const { refreshUser } = useAuth()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [photoError, setPhotoError] = useState<string | null>(null)

  const { data: profile, isPending, isError, error } = useQuery({
    queryKey: queryKeys.members.me(),
    queryFn: getMyProfile,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<UpdateMyProfileForm>({
    resolver: zodResolver(updateMyProfileSchema),
    defaultValues: { phoneNumber: '' },
  })

  useEffect(() => {
    if (profile) reset({ phoneNumber: profile.phoneNumber ?? '' })
  }, [profile, reset])

  const saveProfile = useMutation({
    mutationFn: updateMyProfile,
    onSuccess: async (updated) => {
      queryClient.setQueryData(queryKeys.members.me(), updated)
      reset({ phoneNumber: updated.phoneNumber ?? '' })
      // The shell greets the member by name and role, both sourced from /auth/me.
      await refreshUser()
    },
  })

  const savePhoto = useMutation({
    mutationFn: updateMyPhoto,
    onSuccess: async (updated) => {
      queryClient.setQueryData(queryKeys.members.me(), updated)
      // Lists render the avatar too, and their cached rows still point at the old photo.
      await queryClient.invalidateQueries({ queryKey: queryKeys.members.all })
    },
  })

  const handlePhotoPicked = (file: File | undefined) => {
    setPhotoError(null)
    if (!file) return

    if (!(IMAGE_CONTENT_TYPES as readonly string[]).includes(file.type)) {
      setPhotoError('Позволени са само JPEG, PNG и HEIC изображения.')
      return
    }

    if (file.size > MAX_PHOTO_BYTES) {
      setPhotoError('Снимката не може да е по-голяма от 5 MB.')
      return
    }

    savePhoto.mutate(file)
  }

  if (isPending) return <Spinner />
  if (isError) return <Alert tone="error">{errorMessage(error)}</Alert>

  return (
    <div className="max-w-xl space-y-5">
      <h1 className="text-2xl font-semibold text-slate-900">Моят профил</h1>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex items-center gap-5">
          <Avatar photoUrl={profile.photoUrl} fullName={profile.fullName} size="lg" />

          <div className="min-w-0">
            <p className="font-medium text-slate-900">{profile.fullName}</p>
            <p className="mt-0.5 text-sm text-slate-500">{profile.email}</p>

            <Button
              variant="secondary"
              size="sm"
              className="mt-3"
              loading={savePhoto.isPending}
              onClick={() => fileInputRef.current?.click()}
            >
              <Upload aria-hidden className="size-4" />
              Смени снимката
            </Button>

            <input
              ref={fileInputRef}
              type="file"
              accept={IMAGE_CONTENT_TYPES.join(',')}
              className="hidden"
              onChange={(event) => {
                handlePhotoPicked(event.target.files?.[0])
                // Reset so picking the same file again still fires a change event.
                event.target.value = ''
              }}
            />
          </div>
        </div>

        {photoError && (
          <Alert tone="error" className="mt-4">
            {photoError}
          </Alert>
        )}
        {savePhoto.isError && (
          <Alert tone="error" className="mt-4">
            {errorMessage(savePhoto.error)}
          </Alert>
        )}
      </section>

      <section className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">Данни</h2>

        <dl className="mb-5 grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-slate-500">Роля</dt>
            <dd className="mt-1">
              <Badge tone={roleTone(profile.role)}>{SYSTEM_ROLE_LABELS[profile.role]}</Badge>
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Отдел</dt>
            <dd className="mt-0.5 font-medium text-slate-900">
              {profile.department
                ? (profile.departmentName ?? DEPARTMENT_CODE_LABELS[profile.department])
                : 'Организационно ниво'}
            </dd>
          </div>
          <div>
            <dt className="text-slate-500">Присъединен на</dt>
            <dd className="mt-0.5 font-medium text-slate-900">{formatDate(profile.joinedOn)}</dd>
          </div>
        </dl>

        <form
          onSubmit={handleSubmit((values) => saveProfile.mutate(values))}
          noValidate
          className="space-y-4 border-t border-slate-200 pt-5"
        >
          {saveProfile.isError && <Alert tone="error">{errorMessage(saveProfile.error)}</Alert>}
          {saveProfile.isSuccess && !isDirty && <Alert tone="success">Профилът е запазен.</Alert>}

          <Input
            label="Телефон"
            type="tel"
            hint="Единственото поле, което можеш да променяш сам. Празно поле го изчиства."
            error={errors.phoneNumber?.message}
            {...register('phoneNumber')}
          />

          <div className="flex justify-end">
            <Button type="submit" loading={saveProfile.isPending} disabled={!isDirty}>
              Запази
            </Button>
          </div>
        </form>
      </section>
    </div>
  )
}
