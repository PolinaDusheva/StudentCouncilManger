import { useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Download, FileText, Trash2, Upload } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { EmptyState } from '@/components/ui/EmptyState'
import { Spinner } from '@/components/ui/Spinner'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import {
  deleteTaskDocument,
  getTaskDocuments,
  MAX_DOCUMENT_BYTES,
  taskDocumentDownloadPath,
  uploadTaskDocument,
} from '@/lib/api/taskDocuments'
import { useAuth } from '@/lib/auth/useAuth'
import type { TaskDocumentDto } from '@/lib/types/dto'
import { DOCUMENT_CONTENT_TYPES } from '@/lib/types/enums'
import { downloadAuthenticatedFile, formatFileSize } from '@/lib/utils/download'
import { formatDateTime } from '@/lib/utils/format'

/** Mirrors the server rule: the uploader, or organisation leadership. */
function canDelete(document: TaskDocumentDto, userId: string, role: string): boolean {
  return document.uploadedBy?.id === userId || role === 'OrgPresident' || role === 'OrgVicePresident'
}

/** Anyone who can see the task may upload; only the uploader or org leadership may delete. */
export function TaskDocuments({ taskId }: { taskId: string }) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [localError, setLocalError] = useState<string | null>(null)
  const [pendingDelete, setPendingDelete] = useState<TaskDocumentDto | null>(null)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const { data: documents, isPending, isError, error } = useQuery({
    queryKey: queryKeys.tasks.documents(taskId),
    queryFn: () => getTaskDocuments(taskId),
  })

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.documents(taskId) })
    // The list and board carry a document count that is now stale.
    await queryClient.invalidateQueries({ queryKey: queryKeys.tasks.lists() })
  }

  const upload = useMutation({
    mutationFn: (file: File) => uploadTaskDocument(taskId, file),
    onSuccess: refresh,
  })

  const remove = useMutation({
    mutationFn: (docId: string) => deleteTaskDocument(taskId, docId),
    onSuccess: async () => {
      setPendingDelete(null)
      await refresh()
    },
  })

  const handlePicked = (file: File | undefined) => {
    setLocalError(null)
    if (!file) return

    // Checked here so an oversized file fails instantly instead of after a long upload;
    // the server enforces the same cap and additionally checks the file's magic bytes.
    if (file.size > MAX_DOCUMENT_BYTES) {
      setLocalError('Файлът не може да е по-голям от 25 MB.')
      return
    }

    upload.mutate(file)
  }

  const handleDownload = async (document: TaskDocumentDto) => {
    setDownloadError(null)
    try {
      await downloadAuthenticatedFile(
        taskDocumentDownloadPath(taskId, document.id),
        document.originalFileName,
      )
    } catch (cause) {
      setDownloadError(errorMessage(cause))
    }
  }

  return (
    <Card variant="panel">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="font-serif text-[22px] leading-[1.3] font-normal text-ink">
          Документи {documents && `(${documents.length})`}
        </h2>

        <Button
          variant="secondary"
          size="sm"
          loading={upload.isPending}
          onClick={() => fileInputRef.current?.click()}
        >
          <Upload aria-hidden className="size-4" />
          Качи файл
        </Button>

        <input
          ref={fileInputRef}
          type="file"
          accept={DOCUMENT_CONTENT_TYPES.join(',')}
          className="hidden"
          onChange={(event) => {
            handlePicked(event.target.files?.[0])
            // Reset so picking the same file again still fires a change event.
            event.target.value = ''
          }}
        />
      </div>

      {localError && (
        <Alert tone="error" className="mb-4">
          {localError}
        </Alert>
      )}
      {upload.isError && (
        <Alert tone="error" className="mb-4">
          {errorMessage(upload.error)}
        </Alert>
      )}
      {downloadError && (
        <Alert tone="error" className="mb-4">
          {downloadError}
        </Alert>
      )}
      {remove.isError && (
        <Alert tone="error" className="mb-4">
          {errorMessage(remove.error)}
        </Alert>
      )}

      {isPending ? (
        <Spinner />
      ) : isError ? (
        <Alert tone="error">{errorMessage(error)}</Alert>
      ) : documents.length === 0 ? (
        <EmptyState title="Няма прикачени документи" description="До 25 MB на файл." />
      ) : (
        <ul className="divide-y divide-divider">
          {documents.map((document) => (
            <li key={document.id} className="flex items-center gap-3 py-3">
              <FileText aria-hidden className="size-5 shrink-0 text-faint" />

              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-ink">
                  {document.originalFileName}
                </p>
                <p className="text-xs text-muted">
                  {formatFileSize(document.sizeBytes)}
                  {document.uploadedBy && ` · ${document.uploadedBy.fullName}`}
                  {` · ${formatDateTime(document.uploadedAtUtc)}`}
                </p>
              </div>

              <Button
                variant="ghost"
                size="sm"
                onClick={() => void handleDownload(document)}
                aria-label={`Свали ${document.originalFileName}`}
              >
                <Download aria-hidden className="size-4" />
              </Button>

              {user && canDelete(document, user.id, user.role) && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setPendingDelete(document)}
                  aria-label={`Изтрий ${document.originalFileName}`}
                  className="text-danger hover:bg-tone-danger-bg"
                >
                  <Trash2 aria-hidden className="size-4" />
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Изтриване на документ"
        message={`„${pendingDelete?.originalFileName}“ ще бъде изтрит завинаги.`}
        confirmLabel="Изтрий"
        loading={remove.isPending}
        onConfirm={() => pendingDelete && remove.mutate(pendingDelete.id)}
        onCancel={() => setPendingDelete(null)}
      />
    </Card>
  )
}
