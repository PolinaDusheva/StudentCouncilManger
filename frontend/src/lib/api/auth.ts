/**
 * `/api/v1/auth` — request schemas and calls.
 *
 * Each schema mirrors the matching complexType in `.ai/api-requests.xsd` section 4, and
 * the inferred type is what the corresponding call sends as its JSON body.
 */

import { z } from 'zod'

import { emailSchema, nonEmptyString, passwordSchema } from '@/lib/validation/common'
import type { AuthTokensResponse, MeResponse } from '@/lib/types/dto'

import { apiFetch } from './client'

/** XSD `LoginRequest` — the password is only required to be non-empty at sign-in. */
export const loginSchema = z.object({
  email: emailSchema,
  password: nonEmptyString('Паролата е задължителна.'),
})
export type LoginRequest = z.infer<typeof loginSchema>

/** XSD `ChangePasswordRequest`. */
export const changePasswordSchema = z
  .object({
    currentPassword: nonEmptyString('Текущата парола е задължителна.'),
    newPassword: passwordSchema,
    confirmPassword: nonEmptyString('Потвърди новата парола.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Паролите не съвпадат.',
    path: ['confirmPassword'],
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    message: 'Новата парола трябва да е различна от текущата.',
    path: ['newPassword'],
  })
export type ChangePasswordForm = z.infer<typeof changePasswordSchema>

/** XSD `ForgotPasswordRequest`. */
export const forgotPasswordSchema = z.object({ email: emailSchema })
export type ForgotPasswordRequest = z.infer<typeof forgotPasswordSchema>

/** XSD `ResetPasswordRequest` — `token` arrives in the emailed link. */
export const resetPasswordSchema = z
  .object({
    email: emailSchema,
    token: nonEmptyString('Липсва токен за нулиране.'),
    newPassword: passwordSchema,
    confirmPassword: nonEmptyString('Потвърди новата парола.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Паролите не съвпадат.',
    path: ['confirmPassword'],
  })
export type ResetPasswordForm = z.infer<typeof resetPasswordSchema>

// ---------------------------------------------------------------- calls

/** `POST /auth/login` — anonymous, rate-limited by the `auth` policy. */
export function login(request: LoginRequest): Promise<AuthTokensResponse> {
  return apiFetch<AuthTokensResponse>('/auth/login', { method: 'POST', body: request })
}

/** `POST /auth/logout` — idempotently revokes the supplied refresh token. */
export function logout(refreshToken: string): Promise<void> {
  return apiFetch<void>('/auth/logout', { method: 'POST', body: { refreshToken } })
}

/** `POST /auth/change-password` — clears the `mustChangePassword` flag on success. */
export function changePassword(request: { currentPassword: string; newPassword: string }): Promise<void> {
  return apiFetch<void>('/auth/change-password', { method: 'POST', body: request })
}

/** `POST /auth/forgot-password` — always succeeds, so accounts cannot be enumerated. */
export function forgotPassword(request: ForgotPasswordRequest): Promise<void> {
  return apiFetch<void>('/auth/forgot-password', { method: 'POST', body: request })
}

/** `POST /auth/reset-password` — sets a new password against an emailed token. */
export function resetPassword(request: {
  email: string
  token: string
  newPassword: string
}): Promise<void> {
  return apiFetch<void>('/auth/reset-password', { method: 'POST', body: request })
}

/** `GET /auth/me` — profile, role, department and permission flags. */
export function getMe(): Promise<MeResponse> {
  return apiFetch<MeResponse>('/auth/me')
}
