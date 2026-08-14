import { ApiError, NetworkError, type ProblemDetails } from '@/lib/api/problem'
import { getTokens } from '@/lib/auth/tokenStorage'

/**
 * Downloads a bearer-protected file and hands it to the browser as a save.
 *
 * A plain `<a href>` cannot be used: the browser sends no `Authorization` header for a
 * navigation, so the endpoint would answer 401. The bytes are fetched by hand and offered
 * through a temporary object URL, revoked immediately after the click.
 *
 * @param path      absolute API path, e.g. `/api/v1/tasks/{id}/documents/{docId}/download`
 * @param fileName  name to save as; the server also sets one via Content-Disposition
 */
export async function downloadAuthenticatedFile(path: string, fileName: string): Promise<void> {
  const token = getTokens()?.accessToken
  if (!token) {
    throw new ApiError(401, { code: 'unauthorized' })
  }

  let response: Response
  try {
    response = await fetch(path, { headers: { Authorization: `Bearer ${token}` } })
  } catch (cause) {
    throw new NetworkError(cause)
  }

  if (!response.ok) {
    let problem: ProblemDetails = { status: response.status, title: response.statusText }
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      // Streaming endpoints may fail without a ProblemDetails body.
    }
    throw new ApiError(response.status, problem)
  }

  const objectUrl = URL.createObjectURL(await response.blob())

  try {
    const link = document.createElement('a')
    link.href = objectUrl
    link.download = fileName
    document.body.append(link)
    link.click()
    link.remove()
  } finally {
    // Safari needs the URL to outlive the click, so it is released on the next tick.
    setTimeout(() => URL.revokeObjectURL(objectUrl), 0)
  }
}

/** Human-readable file size, e.g. `1,4 MB`. */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`

  const units = ['KB', 'MB', 'GB']
  let value = bytes / 1024
  let unitIndex = 0

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024
    unitIndex += 1
  }

  return `${value.toFixed(value < 10 ? 1 : 0).replace('.', ',')} ${units[unitIndex]}`
}
