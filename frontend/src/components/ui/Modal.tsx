import { useEffect, useRef, type ReactNode } from 'react'
import { X } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  /** Button row pinned to the bottom. */
  footer?: ReactNode
  className?: string
}

/**
 * Dialog built on the native `<dialog>` element, which supplies focus trapping, `Esc`
 * dismissal and top-layer stacking without a library or manual focus bookkeeping.
 */
export function Modal({ open, onClose, title, children, footer, className }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    // showModal()/close() throw if the dialog is already in the requested state.
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    // Fires on Esc; preventDefault keeps the element's state owned by the `open` prop.
    const handleCancel = (event: Event) => {
      event.preventDefault()
      onClose()
    }

    dialog.addEventListener('cancel', handleCancel)
    return () => dialog.removeEventListener('cancel', handleCancel)
  }, [onClose])

  return (
    <dialog
      ref={dialogRef}
      // A click landing on the dialog itself is a click on the backdrop: the padding-free
      // element is fully covered by its content wrapper, which stops propagation.
      onClick={(event) => {
        if (event.target === dialogRef.current) onClose()
      }}
      className={cn(
        'w-[calc(100vw-2rem)] max-w-lg rounded-xl bg-white p-0 shadow-xl',
        'backdrop:bg-slate-900/40',
        // `m-auto` is required, not cosmetic: the UA stylesheet centres a modal dialog with
        // `margin: auto`, and Tailwind's preflight resets every margin to 0 — without this
        // the dialog sticks to the top-left corner.
        'm-auto border-0 text-slate-900',
        className,
      )}
    >
      <div className="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4">
        <h2 className="text-base font-semibold">{title}</h2>
        <button
          type="button"
          onClick={onClose}
          aria-label="Затвори"
          className="-m-1 rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
        >
          <X aria-hidden className="size-4" />
        </button>
      </div>

      <div className="px-5 py-4">{children}</div>

      {footer && (
        <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-4">{footer}</div>
      )}
    </dialog>
  )
}
