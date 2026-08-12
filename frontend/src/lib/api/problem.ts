/**
 * RFC 7807 ProblemDetails, as produced by the backend's GlobalExceptionHandler.
 *
 * Every handled failure carries a machine-readable `code` extension
 * (`invalid_credentials`, `password_change_required`, `not_found`, ...); validation
 * failures additionally carry an `errors` map keyed by field name.
 */

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  /** Machine-readable error code, e.g. `invalid_credentials`. */
  code?: string
  traceId?: string
  /** Present on ValidationProblemDetails: field name → messages. */
  errors?: Record<string, string[]>
}

/** Error thrown by `apiFetch` for any non-2xx response (or a network failure). */
export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly problem: ProblemDetails
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? problem.detail ?? `Заявката се провали (${status}).`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    this.code = problem.code ?? codeFromStatus(status)
    this.fieldErrors = problem.errors ?? {}
  }

  /** True when the failure is a 400 carrying per-field validation messages. */
  get isValidation(): boolean {
    return Object.keys(this.fieldErrors).length > 0
  }
}

/** A network-level failure (server unreachable, request aborted by the browser). */
export class NetworkError extends Error {
  constructor(cause?: unknown) {
    super('Няма връзка със сървъра. Провери дали backend-ът работи.')
    this.name = 'NetworkError'
    this.cause = cause
  }
}

function codeFromStatus(status: number): string {
  switch (status) {
    case 401:
      return 'unauthorized'
    case 403:
      return 'forbidden'
    case 404:
      return 'not_found'
    case 409:
      return 'conflict'
    case 413:
      return 'payload_too_large'
    case 423:
      return 'account_locked'
    case 429:
      return 'rate_limited'
    default:
      return status >= 500 ? 'internal_error' : 'bad_request'
  }
}

/** Bulgarian messages for the error codes the UI surfaces directly. */
const MESSAGES: Record<string, string> = {
  invalid_credentials: 'Грешен имейл или парола.',
  account_inactive: 'Акаунтът е деактивиран. Свържи се с администратор.',
  account_locked: 'Твърде много неуспешни опити. Акаунтът е заключен за няколко минути.',
  password_change_required: 'Трябва да смениш паролата си, преди да продължиш.',
  invalid_reset_token: 'Линкът за нулиране е невалиден или изтекъл. Заяви нов.',
  unauthorized: 'Сесията изтече. Влез отново.',
  forbidden: 'Нямаш права за това действие.',
  not_found: 'Ресурсът не е намерен.',
  rate_limited: 'Твърде много заявки. Опитай отново след малко.',
  payload_too_large: 'Файлът е твърде голям (максимум 25 MB).',
  internal_error: 'Възникна неочаквана грешка. Опитай отново.',
}

/** Turns any thrown value into a message safe to render in the UI. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return MESSAGES[error.code] ?? error.message
  }
  if (error instanceof NetworkError) {
    return error.message
  }
  if (error instanceof Error) {
    return error.message
  }
  return 'Възникна неочаквана грешка.'
}
