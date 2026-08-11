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
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
      </label>

      <select
        ref={ref}
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={cn(
          'block w-full rounded-lg border-0 bg-white px-3 py-2 text-sm text-slate-900 shadow-sm',
          'ring-1 ring-slate-300 ring-inset',
          'focus:ring-brand-500 focus:ring-2 focus:ring-inset focus:outline-none',
          'disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500',
          error && 'ring-red-500 focus:ring-red-500',
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
        <p id={errorId} role="alert" className="text-sm text-red-600">
          {error}
        </p>
      ) : hint ? (
        <p id={hintId} className="text-sm text-slate-500">
          {hint}
        </p>
      ) : null}
    </div>
  )
})
