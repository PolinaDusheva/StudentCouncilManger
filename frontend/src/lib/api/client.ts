/**
 * Thin fetch wrapper around the Student Council API.
 *
 * Responsibilities:
 *  - prefixes `/api/v1` (proxied to the .NET backend by the Vite dev server),
 *  - attaches the bearer token,
 *  - turns non-2xx responses into `ApiError` carrying the ProblemDetails `code`,
 *  - transparently refreshes an expired access token once and replays the request.
 */

import { clearTokens, getTokens, setTokens } from '@/lib/auth/tokenStorage'
import type { AuthTokensResponse } from '@/lib/types/dto'

import { ApiError, NetworkError, type ProblemDetails } from './problem'

/**
 * Base URL of the API. Empty by default so requests stay same-origin and hit the dev
 * proxy; set `VITE_API_BASE_URL` when the frontend is served from a different origin
 * than the API (which additionally requires CORS on the backend).
 */
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''
const API_PREFIX = '/api/v1'

/** Endpoints that must never carry a bearer token or trigger a refresh attempt. */
const ANONYMOUS_PATHS = ['/auth/login', '/auth/refresh', '/auth/forgot-password', '/auth/reset-password']

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  /** Serialised as a JSON body. Mutually exclusive with `formData`. */
  body?: unknown
  /** Sent as multipart/form-data (file uploads). */
  formData?: FormData
  /** Appended as a query string; `undefined`, `null` and `''` values are dropped. */
  query?: QueryParams
  signal?: AbortSignal
}

export type QueryParams = Record<string, string | number | boolean | null | undefined>

/** Called when the session cannot be recovered, so the app can drop to the login screen. */
let onSessionExpired: (() => void) | null = null

export function setSessionExpiredHandler(handler: (() => void) | null): void {
  onSessionExpired = handler
}

export function buildQueryString(query: QueryParams): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') continue
    params.set(key, String(value))
  }
  const serialised = params.toString()
  return serialised ? `?${serialised}` : ''
}

/**
 * Performs an API call and returns the parsed JSON body, or `undefined` for 204 responses.
 * Throws `ApiError` for HTTP failures and `NetworkError` when the server is unreachable.
 */
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const isAnonymous = ANONYMOUS_PATHS.includes(path)
  const response = await send(path, options, isAnonymous ? null : getTokens()?.accessToken)

  // An expired access token is recoverable exactly once: rotate the pair, then replay.
  if (response.status === 401 && !isAnonymous) {
    const refreshed = await refreshAccessToken()
    if (!refreshed) {
      clearTokens()
      onSessionExpired?.()
      throw new ApiError(401, await readProblem(response))
    }

    const retried = await send(path, options, refreshed)
    return handle<T>(retried)
  }

  return handle<T>(response)
}

async function send(path: string, options: RequestOptions, accessToken: string | null | undefined) {
  const { method = 'GET', body, formData, query, signal } = options

  const headers = new Headers({ Accept: 'application/json' })
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)

  let payload: BodyInit | undefined
  if (formData) {
    // Let the browser set the multipart boundary — never set Content-Type by hand here.
    payload = formData
  } else if (body !== undefined) {
    headers.set('Content-Type', 'application/json')
    payload = JSON.stringify(body)
  }

  const url = `${BASE_URL}${API_PREFIX}${path}${query ? buildQueryString(query) : ''}`

  try {
    return await fetch(url, { method, headers, body: payload, signal })
  } catch (cause) {
    // Propagate cancellation untouched so TanStack Query can tell it apart from a failure.
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause
    throw new NetworkError(cause)
  }
}

async function handle<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response))
  }

  if (response.status === 204 || response.headers.get('Content-Length') === '0') {
    return undefined as T
  }

  const contentType = response.headers.get('Content-Type') ?? ''
  if (!contentType.includes('json')) {
    return undefined as T
  }

  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    const problem = (await response.json()) as ProblemDetails
    return { ...problem, status: problem.status ?? response.status }
  } catch {
    // 429 from the rate limiter and proxy-level errors have no ProblemDetails body.
    return { status: response.status, title: response.statusText }
  }
}

/**
 * Single-flight refresh: concurrent 401s share one `POST /auth/refresh` call, because the
 * backend rotates (and therefore invalidates) the refresh token on every use.
 * Resolves to the new access token, or `null` when the session is gone for good.
 */
let refreshInFlight: Promise<string | null> | null = null

function refreshAccessToken(): Promise<string | null> {
  refreshInFlight ??= performRefresh().finally(() => {
    refreshInFlight = null
  })
  return refreshInFlight
}

async function performRefresh(): Promise<string | null> {
  const tokens = getTokens()
  if (!tokens) return null

  try {
    const response = await send(
      '/auth/refresh',
      { method: 'POST', body: { refreshToken: tokens.refreshToken } },
      null,
    )
    if (!response.ok) return null

    const rotated = (await response.json()) as AuthTokensResponse
    setTokens({ accessToken: rotated.accessToken, refreshToken: rotated.refreshToken })
    return rotated.accessToken
  } catch {
    return null
  }
}
