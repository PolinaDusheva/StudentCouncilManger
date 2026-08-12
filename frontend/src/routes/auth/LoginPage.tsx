import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { zodResolver } from '@hookform/resolvers/zod'

import { AuthLayout } from '@/components/layout/AuthLayout'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { loginSchema, type LoginRequest } from '@/lib/api/auth'
import { errorMessage } from '@/lib/api/problem'
import { useAuth } from '@/lib/auth/useAuth'
import { HOME_PATH } from '@/routes/guards'

/** Set by the password screens after they end the session, to explain the redirect. */
interface LoginLocationState {
  from?: { pathname: string }
  notice?: string
}

export function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as LoginLocationState | null

  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginRequest>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await signIn(values)
      // A member who must still change their password is redirected by RequireAuth.
      navigate(state?.from?.pathname ?? HOME_PATH, { replace: true })
    } catch (error) {
      setFormError(errorMessage(error))
    }
  })

  return (
    <AuthLayout
      title="Студентски съвет"
      subtitle="Влез с университетския си имейл"
      footer={
        <Link to="/forgot-password" className="text-brand-700 font-medium hover:underline">
          Забравена парола?
        </Link>
      }
    >
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {state?.notice && <Alert tone="success">{state.notice}</Alert>}
        {formError && <Alert tone="error">{formError}</Alert>}

        <Input
          label="Имейл"
          type="email"
          autoComplete="username"
          autoFocus
          error={errors.email?.message}
          {...register('email')}
        />

        <Input
          label="Парола"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />

        <Button type="submit" loading={isSubmitting} className="w-full">
          Вход
        </Button>
      </form>
    </AuthLayout>
  )
}

/** Exported so the password screens can redirect here carrying a notice. */
export type { LoginLocationState }
