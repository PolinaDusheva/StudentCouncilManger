import type { ReactNode } from 'react'
import { AlertCircle, AlertTriangle, CheckCircle2, Info } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

type Tone = 'error' | 'warning' | 'success' | 'info'

const TONES: Record<Tone, { box: string; icon: typeof Info }> = {
  error: { box: 'bg-tone-danger-bg text-tone-danger-alert-text ring-tone-danger-border', icon: AlertCircle },
  // For outcomes that succeeded but need attention — e.g. an event saved with schedule overlaps.
  warning: {
    box: 'bg-tone-warning-bg text-tone-warning-alert-text ring-tone-warning-border',
    icon: AlertTriangle,
  },
  success: { box: 'bg-tone-success-bg text-tone-success-alert-text ring-tone-success-border', icon: CheckCircle2 },
  info: { box: 'bg-tone-info-bg text-tone-info-alert-text ring-tone-info-border', icon: Info },
}

interface AlertProps {
  tone?: Tone
  children: ReactNode
  className?: string
}

export function Alert({ tone = 'info', children, className }: AlertProps) {
  const { box, icon: Icon } = TONES[tone]

  return (
    <div
      // Errors interrupt; confirmations wait for a pause in the screen reader's output.
      role={tone === 'error' ? 'alert' : 'status'}
      className={cn('flex gap-2.5 rounded-[15px] px-3.5 py-3 text-sm ring-1 ring-inset', box, className)}
    >
      <Icon aria-hidden className="mt-0.5 size-4 shrink-0" />
      <div className="min-w-0">{children}</div>
    </div>
  )
}
