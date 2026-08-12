/**
 * Reusable Zod pieces mirroring the simple types in `.ai/api-requests.xsd` section 1.
 *
 * Client-side validation exists to give fast feedback — the server re-validates
 * everything. Where the XSD is deliberately stricter than the server (it says so in the
 * relevant annotations), the schema follows the XSD.
 */

import { z } from 'zod'

/** XSD `Email`: 3–256 chars, exactly one `@` that is neither first nor last. */
export const emailSchema = z
  .string()
  .trim()
  .min(1, 'Имейлът е задължителен.')
  .max(256, 'Имейлът не може да е по-дълъг от 256 символа.')
  .regex(/^[^@]+@[^@]+$/, 'Въведи валиден имейл адрес.')

/**
 * XSD `Password` (PasswordRules.Password): at least 8 characters, at least one uppercase
 * letter and at least one digit. Checked as three separate rules so the user sees exactly
 * which one fails.
 */
export const passwordSchema = z
  .string()
  .min(8, 'Паролата трябва да е поне 8 символа.')
  .regex(/[A-Z]/, 'Паролата трябва да съдържа поне една главна буква.')
  .regex(/[0-9]/, 'Паролата трябва да съдържа поне една цифра.')

/** XSD `NonEmptyString`: FluentValidation `.NotEmpty()`, no upper bound. */
export const nonEmptyString = (message: string) => z.string().min(1, message)

/** XSD `Guid`: canonical 8-4-4-4-12 form. */
export const guidSchema = z
  .string()
  .regex(
    /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/,
    'Невалиден идентификатор.',
  )
