import { useEffect, useState } from 'react'

import { getTokens } from '@/lib/auth/tokenStorage'

/**
 * Fetches a bearer-protected image and exposes it as a blob URL.
 *
 * `GET /members/{id}/photo` requires an `Authorization` header, which the browser does not
 * send for `<img src>`. So the bytes are fetched by hand and handed over as an object URL,
 * revoked on unmount so the blob is not leaked.
 *
 * `path` is the relative path the API returns in `photoUrl`
 * (e.g. `/api/v1/members/{id}/photo`), or null when the member has no photo.
 *
 * Deliberately not a TanStack Query cache entry: object URLs have a manual lifecycle
 * (`revokeObjectURL`) that does not survive a cache outliving the component.
 */
export function useAuthenticatedImage(path: string | null | undefined): string | null {
  const [objectUrl, setObjectUrl] = useState<string | null>(null)

  useEffect(() => {
    const token = getTokens()?.accessToken
    if (!path || !token) {
      setObjectUrl(null)
      return
    }

    // Guards against out-of-order responses when `path` changes quickly, and against
    // setting state after unmount.
    const controller = new AbortController()
    let created: string | null = null

    void (async () => {
      try {
        const response = await fetch(path, {
          headers: { Authorization: `Bearer ${token}` },
          signal: controller.signal,
        })
        if (!response.ok) return

        created = URL.createObjectURL(await response.blob())
        setObjectUrl(created)
      } catch {
        // A missing or forbidden photo is not an error — Avatar falls back to initials.
      }
    })()

    return () => {
      controller.abort()
      if (created) URL.revokeObjectURL(created)
    }
  }, [path])

  return objectUrl
}
