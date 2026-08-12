import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { zodResolver } from '@hookform/resolvers/zod'

import { AuthLayout } from '@/components/layout/AuthLayout'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { changePassword, changePasswordSchema, type ChangePasswordForm } from '@/lib/api/auth'
import { ApiError, errorMessage } from '@/lib/api/problem'
import { useAuth } from '@/lib/auth/useAuth'
import { LOGIN_PATH } from '@/routes/guards'

import type { LoginLocationState } from './LoginPage'

/**
 * First-login password change. Reached automatically whenever `mustChangePassword` is set,
 * because until then the API rejects every other endpoint with 403 `password_change_required`.
 *
 * On success the server rotates the security stamp and revokes all refresh tokens, so the
 * current session is already invalid — the only correct next step is a fresh sign-in.
 */
export function ChangePasswordPage() {
  const { user, mustChangePassword, endSession } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordForm>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
      })

      endSession()
      const state: LoginLocationState = {
        notice: 'Паролата е сменена успешно. Влез с новата парола.',
      }
      navigate(LOGIN_PATH, { replace: true, state })
    } catch (error) {
      // A wrong current password comes back as a field-level validation error.
      if (error instanceof ApiError && error.fieldErrors.currentPassword) {
        setError('currentPassword', { message: 'Текущата парола е грешна.' })
        return
      }
      setFormError(errorMessage(error))
    }
  })

  return (
    <AuthLayout
      title={mustChangePassword ? 'Смени временната парола' : 'Смяна на парола'}
      subtitle={
        mustChangePassword
          ? 'Влезе с временна парола. Задай своя, за да продължиш.'
          : user?.email
      }
    >
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {formError && <Alert tone="error">{formError}</Alert>}

        <Input
          label="Текуща парола"
          type="password"
          autoComplete="current-password"
          autoFocus
          error={errors.currentPassword?.message}
          {...register('currentPassword')}
        />

        <Input
          label="Нова парола"
          type="password"
          autoComplete="new-password"
          hint="Минимум 8 символа, поне една главна буква и една цифра."
          error={errors.newPassword?.message}
          {...register('newPassword')}
        />

        <Input
          label="Потвърди новата парола"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />

        <Alert tone="info">След смяната сесията се прекратява и трябва да влезеш отново.</Alert>

        <Button type="submit" loading={isSubmitting} className="w-full">
          Смени паролата
        </Button>
      </form>
    </AuthLayout>
  )
}
