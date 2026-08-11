import { http, HttpResponse } from 'msw'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { clearTokens, getTokens, setTokens } from '@/lib/auth/tokenStorage'
import { server } from '@/test/server'

import { apiFetch, setSessionExpiredHandler } from './client'
import { ApiError } from './problem'

// The client calls relative paths; jsdom resolves them against http://localhost/.
const API = 'http://localhost/api/v1'

/** The rotated pair returned by a successful POST /auth/refresh. */
const REFRESHED = {
  accessToken: 'new-access',
  refreshToken: 'new-refresh',
  expiresInSeconds: 7200,
  mustChangePassword: false,
  user: { id: 'm-1' },
}

afterEach(() => {
  clearTokens()
  setSessionExpiredHandler(null)
})

describe('apiFetch', () => {
  it('слага bearer токена на заявката', async () => {
    setTokens({ accessToken: 'access-1', refreshToken: 'refresh-1' })
    let seen: string | null = null

    server.use(
      http.get(`${API}/auth/me`, ({ request }) => {
        seen = request.headers.get('Authorization')
        return HttpResponse.json({ id: 'm-1' })
      }),
    )

    await apiFetch('/auth/me')
    expect(seen).toBe('Bearer access-1')
  })

  it('връща undefined при 204, без да пука на празно тяло', async () => {
    setTokens({ accessToken: 'access-1', refreshToken: 'refresh-1' })
    server.use(http.post(`${API}/auth/logout`, () => new HttpResponse(null, { status: 204 })))

    await expect(apiFetch('/auth/logout', { method: 'POST', body: {} })).resolves.toBeUndefined()
  })

  it('обновява токена при 401 и повтаря заявката', async () => {
    setTokens({ accessToken: 'expired', refreshToken: 'refresh-1' })

    server.use(
      http.get(`${API}/auth/me`, ({ request }) =>
        request.headers.get('Authorization') === 'Bearer new-access'
          ? HttpResponse.json({ id: 'm-1' })
          : HttpResponse.json({ code: 'unauthorized' }, { status: 401 }),
      ),
      http.post(`${API}/auth/refresh`, () => HttpResponse.json(REFRESHED)),
    )

    await expect(apiFetch<{ id: string }>('/auth/me')).resolves.toEqual({ id: 'm-1' })

    // Ротираната двойка трябва да е записана — старият refresh токен вече е невалиден сървърно.
    expect(getTokens()).toEqual({ accessToken: 'new-access', refreshToken: 'new-refresh' })
  })

  it('обновява само веднъж при няколко едновременни 401-ци', async () => {
    setTokens({ accessToken: 'expired', refreshToken: 'refresh-1' })
    let refreshCalls = 0

    server.use(
      http.get(`${API}/members`, ({ request }) =>
        request.headers.get('Authorization') === 'Bearer new-access'
          ? HttpResponse.json({ items: [] })
          : HttpResponse.json({ code: 'unauthorized' }, { status: 401 }),
      ),
      http.get(`${API}/departments`, ({ request }) =>
        request.headers.get('Authorization') === 'Bearer new-access'
          ? HttpResponse.json([])
          : HttpResponse.json({ code: 'unauthorized' }, { status: 401 }),
      ),
      http.post(`${API}/auth/refresh`, () => {
        refreshCalls += 1
        return HttpResponse.json(REFRESHED)
      }),
    )

    await Promise.all([apiFetch('/members'), apiFetch('/departments')])

    // Сървърът анулира refresh токена при ползване — второ извикване би убило сесията.
    expect(refreshCalls).toBe(1)
  })

  it('чисти сесията и уведомява, когато обновяването се провали', async () => {
    setTokens({ accessToken: 'expired', refreshToken: 'dead' })
    const onExpired = vi.fn()
    setSessionExpiredHandler(onExpired)

    server.use(
      http.get(`${API}/auth/me`, () => HttpResponse.json({ code: 'unauthorized' }, { status: 401 })),
      http.post(`${API}/auth/refresh`, () =>
        HttpResponse.json({ code: 'unauthorized' }, { status: 401 }),
      ),
    )

    await expect(apiFetch('/auth/me')).rejects.toBeInstanceOf(ApiError)
    expect(getTokens()).toBeNull()
    expect(onExpired).toHaveBeenCalledOnce()
  })

  it('не праща токен и не обновява при анонимните ендпойнти', async () => {
    setTokens({ accessToken: 'access-1', refreshToken: 'refresh-1' })
    let seen: string | null = 'unchanged'

    server.use(
      http.post(`${API}/auth/login`, ({ request }) => {
        seen = request.headers.get('Authorization')
        return HttpResponse.json({ code: 'invalid_credentials' }, { status: 401 })
      }),
    )

    await expect(
      apiFetch('/auth/login', { method: 'POST', body: { email: 'a@b', password: 'x' } }),
    ).rejects.toMatchObject({ code: 'invalid_credentials' })

    expect(seen).toBeNull()
    // Сесията остава — 401 при логин не означава изтекла сесия.
    expect(getTokens()).not.toBeNull()
  })

  it('пропуска празни стойности от query string-а', async () => {
    setTokens({ accessToken: 'access-1', refreshToken: 'refresh-1' })
    let url = ''

    server.use(
      http.get(`${API}/members`, ({ request }) => {
        url = request.url
        return HttpResponse.json({ items: [] })
      }),
    )

    await apiFetch('/members', {
      query: { search: '', page: 1, department: undefined, status: 'Active' },
    })

    expect(url).toContain('page=1')
    expect(url).toContain('status=Active')
    expect(url).not.toContain('search=')
    expect(url).not.toContain('department=')
  })
})
