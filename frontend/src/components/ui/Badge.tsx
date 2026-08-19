import type { ReactNode } from 'react'

import { cn } from '@/lib/utils/cn'

export type BadgeTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info'

const TONES: Record<BadgeTone, string> = {
  neutral: 'bg-tone-neutral-bg text-tone-neutral-text ring-tone-neutral-border',
  success: 'bg-tone-success-bg text-tone-success-text ring-tone-success-border',
  warning: 'bg-tone-warning-bg text-tone-warning-text ring-tone-warning-border',
  danger: 'bg-tone-danger-bg text-tone-danger-text ring-tone-danger-border',
  info: 'bg-tone-info-bg text-tone-info-text ring-tone-info-border',
}

interface BadgeProps {
  tone?: BadgeTone
  children: ReactNode
  className?: string
}

/**
 * Compact status label. The tone is chosen by the caller — mapping a role or status to a
 * colour is a screen-level decision, so no default mapping lives here.
 */
export function Badge({ tone = 'neutral', children, className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-lg px-2 py-0.5 text-xs font-semibold ring-1 ring-inset',
        TONES[tone],
        className,
      )}
    >
      {children}
    </span>
  )
}
