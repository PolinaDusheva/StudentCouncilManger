import type { HTMLAttributes, ReactNode } from 'react'

import { cn } from '@/lib/utils/cn'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode
  /** `panel` uses the larger 20px radius and roomier padding for single-section detail screens. */
  variant?: 'default' | 'panel'
}

/**
 * Shared white surface for page sections. Replaces the hand-rolled card shell (rounded corners,
 * soft shadow, hairline border) that used to be duplicated on nearly every route.
 */
export function Card({ children, variant = 'default', className, ...props }: CardProps) {
  return (
    <div
      className={cn(
        'bg-surface shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider',
        variant === 'panel' ? 'rounded-[20px] p-6' : 'rounded-[15px] p-5',
        className,
      )}
      {...props}
    >
      {children}
    </div>
  )
}
