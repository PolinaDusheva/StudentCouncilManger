import { forwardRef, useId, type InputHTMLAttributes, type ReactNode } from 'react'

import { cn } from '@/lib/utils/cn'

interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  label: string
  /** Validation message; renders the field in an error state and announces it. */
  error?: string
  /** Persistent helper text shown when there is no error. */
  hint?: ReactNode
}

/**
 * A labelled text input wired for accessibility: the label is always associated with the
 * control, and errors are linked through `aria-describedby` so screen readers announce them.
 */
export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, error, hint, className, ...props },
  ref,
) {
  const id = useId()
  const errorId = `${id}-error`
  const hintId = `${id}-hint`
  const describedBy = error ? errorId : hint ? hintId : undefined

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-semibold text-ink-soft">
        {label}
      </label>

      <input
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink',
          'shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] placeholder:text-faint',
          'focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-subtle disabled:text-faint',
          error && 'border-danger focus:border-danger focus:shadow-none',
          className,
        )}
        {...props}
      />

      {error ? (
        <p id={errorId} role="alert" className="text-sm text-danger">
          {error}
        </p>
      ) : hint ? (
        <p id={hintId} className="text-sm text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
})
