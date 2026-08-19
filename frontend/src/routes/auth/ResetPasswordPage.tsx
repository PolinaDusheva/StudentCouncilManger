import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { zodResolver } from '@hookform/resolvers/zod'

import { AuthLayout } from '@/components/layout/AuthLayout'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { resetPassword, resetPasswordSchema, type ResetPasswordForm } from '@/lib/api/auth'
import { errorMessage } from '@/lib/api/problem'
import { LOGIN_PATH } from '@/routes/guards'

import type { LoginLocationState } from './LoginPage'

/**
 * Completes a password reset. The emailed link is built by the backend as
 * `{PasswordReset:ResetUrlBase}?email=...&token=...`, so both values arrive as query
 * parameters and are submitted back unchanged.
 */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const linkIsComplete = email !== '' && token !== ''

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordForm>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { email, token, newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await resetPassword({
        email: values.email,
        token: values.token,
        newPassword: values.newPassword,
      })

      const state: LoginLocationState = {
        notice: 'Паролата е нулирана успешно. Влез с новата парола.',
      }
      navigate(LOGIN_PATH, { replace: true, state })
    } catch (error) {
      setFormError(errorMessage(error))
    }
  })

  return (
    <AuthLayout
      title="Нова парола"
      subtitle={linkIsComplete ? email : undefined}
      footer={
        <Link to={LOGIN_PATH} className="text-accent hover:text-accent-hover font-medium hover:underline">
          Обратно към входа
        </Link>
      }
    >
      {linkIsComplete ? (
        <form onSubmit={onSubmit} noValidate className="space-y-4">
          {formError && <Alert tone="error">{formError}</Alert>}

          {/* The identity of the account is fixed by the link, not by the form. */}
          <input type="hidden" {...register('email')} />
          <input type="hidden" {...register('token')} />

          <Input
            label="Нова парола"
            type="password"
            autoComplete="new-password"
            autoFocus
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

          <Button type="submit" loading={isSubmitting} className="w-full">
            Запази новата парола
          </Button>
        </form>
      ) : (
        <Alert tone="error">
          Линкът е непълен или повреден. Заяви нов от{' '}
          <Link to="/forgot-password" className="font-medium underline">
            Забравена парола
          </Link>
          .
        </Alert>
      )}
    </AuthLayout>
  )
}
