import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { zodResolver } from '@hookform/resolvers/zod'

import { AuthLayout } from '@/components/layout/AuthLayout'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { forgotPassword, forgotPasswordSchema, type ForgotPasswordRequest } from '@/lib/api/auth'
import { errorMessage } from '@/lib/api/problem'
import { LOGIN_PATH } from '@/routes/guards'

/**
 * Requests a reset link. The API deliberately answers 200 whether or not the account
 * exists, so the confirmation below must not imply that an email was actually sent.
 */
export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordRequest>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await forgotPassword(values)
      setSent(true)
    } catch (error) {
      setFormError(errorMessage(error))
    }
  })

  return (
    <AuthLayout
      title="Забравена парола"
      subtitle={sent ? undefined : 'Ще изпратим линк за нулиране на паролата.'}
      footer={
        <Link to={LOGIN_PATH} className="text-brand-700 font-medium hover:underline">
          Обратно към входа
        </Link>
      }
    >
      {sent ? (
        <Alert tone="success">
          Ако съществува акаунт с този имейл, изпратихме линк за нулиране. Провери и папката
          „Спам“. Линкът е валиден ограничено време.
        </Alert>
      ) : (
        <form onSubmit={onSubmit} noValidate className="space-y-4">
          {formError && <Alert tone="error">{formError}</Alert>}

          <Input
            label="Имейл"
            type="email"
            autoComplete="username"
            autoFocus
            error={errors.email?.message}
            {...register('email')}
          />

          <Button type="submit" loading={isSubmitting} className="w-full">
            Изпрати линк
          </Button>
        </form>
      )}
    </AuthLayout>
  )
}
