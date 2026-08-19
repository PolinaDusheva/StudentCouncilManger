import { forwardRef, useId, type ReactNode, type SelectHTMLAttributes } from 'react'

import { cn } from '@/lib/utils/cn'

export interface SelectOption {
  value: string
  label: string
}

interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id' | 'children'> {
  label: string
  options: SelectOption[]
  /** Label for the "no choice" entry; adds an option with an empty value when set. */
  placeholder?: string
  error?: string
  hint?: ReactNode
}

/**
 * Labelled dropdown, mirroring {@link Input}'s contract so both behave the same in forms.
 * Forwards its ref so react-hook-form's `register()` can drive it.
 */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { label, options, placeholder, error, hint, className, ...props },
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

      <select
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-xl border-2 border-line bg-surface px-4 py-3 text-sm text-ink',
          'focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-subtle disabled:text-faint',
          error && 'border-danger focus:border-danger focus:shadow-none',
          className,
        )}
        {...props}
      >
        {placeholder !== undefined && <option value="">{placeholder}</option>}
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

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
