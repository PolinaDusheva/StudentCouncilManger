import { Button } from './Button'
import { Modal } from './Modal'

interface ConfirmDialogProps {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  /** `danger` for destructive actions (deactivate, delete). */
  tone?: 'danger' | 'primary'
  loading?: boolean
  onConfirm: () => void
  onCancel: () => void
}

/** Confirmation step in front of an action that is awkward or impossible to undo. */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Потвърди',
  cancelLabel = 'Отказ',
  tone = 'danger',
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <Modal
      open={open}
      // Esc and the backdrop mean "cancel"; a pending request must not be abandoned midway.
      onClose={loading ? () => {} : onCancel}
      title={title}
      className="max-w-md"
      footer={
        <>
          <Button variant="secondary" onClick={onCancel} disabled={loading}>
            {cancelLabel}
          </Button>
          <Button variant={tone} onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <p className="text-sm text-slate-600">{message}</p>
    </Modal>
  )
}
