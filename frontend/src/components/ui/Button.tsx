import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Loader2 } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'
type Size = 'sm' | 'md'

const VARIANTS: Record<Variant, string> = {
  primary:
    'bg-ink text-white hover:bg-accent hover:shadow-[0_5px_15px_rgba(255,60,112,0.3)] disabled:hover:bg-ink disabled:hover:shadow-none',
  secondary:
    'bg-transparent text-ink ring-2 ring-border ring-inset hover:ring-accent hover:text-accent disabled:hover:ring-border disabled:hover:text-ink',
  ghost: 'text-muted hover:bg-page hover:text-ink disabled:hover:bg-transparent disabled:hover:text-muted',
  danger: 'bg-danger text-white hover:bg-danger-hover disabled:hover:bg-danger',
}

const SIZES: Record<Size, string> = {
  sm: 'h-9 px-4 text-sm',
  md: 'h-11 px-5 text-sm',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  /** Shows a spinner and blocks interaction while an action is in flight. */
  loading?: boolean
  children: ReactNode
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  disabled,
  className,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      // A loading button stays focusable but must not fire twice.
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-full font-semibold transition-all',
        'disabled:cursor-not-allowed disabled:opacity-60',
        VARIANTS[variant],
        SIZES[size],
        className,
      )}
      {...props}
    >
      {loading && <Loader2 aria-hidden className="size-4 animate-spin" />}
      {children}
    </button>
  )
}
