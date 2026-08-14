/**
 * `/api/v1/tasks/{taskId}/documents` — task attachments.
 *
 * Uploads are validated three ways by the server: MIME type, file extension, and the file's
 * magic bytes. Declaring an allowed content type is not enough for a mismatched file.
 */

import type { TaskDocumentDto } from '@/lib/types/dto'

import { apiFetch } from './client'

/** Server-side cap (`RequestSizeLimit` on the controller). */
export const MAX_DOCUMENT_BYTES = 25 * 1024 * 1024

/** `GET /tasks/{taskId}/documents`. */
export function getTaskDocuments(taskId: string): Promise<TaskDocumentDto[]> {
  return apiFetch<TaskDocumentDto[]>(`/tasks/${taskId}/documents`)
}

/** `POST /tasks/{taskId}/documents` — multipart, field name `file`. */
export function uploadTaskDocument(taskId: string, file: File): Promise<TaskDocumentDto> {
  const formData = new FormData()
  formData.append('file', file)
  return apiFetch<TaskDocumentDto>(`/tasks/${taskId}/documents`, { method: 'POST', formData })
}

/** `DELETE /tasks/{taskId}/documents/{docId}` — uploader or organisation leadership. */
export function deleteTaskDocument(taskId: string, docId: string): Promise<void> {
  return apiFetch<void>(`/tasks/${taskId}/documents/${docId}`, { method: 'DELETE' })
}

/** Path of the download endpoint; fetched through {@link downloadAuthenticatedFile}. */
export function taskDocumentDownloadPath(taskId: string, docId: string): string {
  return `/api/v1/tasks/${taskId}/documents/${docId}/download`
}
