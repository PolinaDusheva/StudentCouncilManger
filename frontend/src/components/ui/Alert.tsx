import type { ReactNode } from 'react'
import { AlertCircle, AlertTriangle, CheckCircle2, Info } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

type Tone = 'error' | 'warning' | 'success' | 'info'

const TONES: Record<Tone, { box: string; icon: typeof Info }> = {
  error: { box: 'bg-red-50 text-red-800 ring-red-200', icon: AlertCircle },
  // For outcomes that succeeded but need attention — e.g. an event saved with schedule overlaps.
  warning: { box: 'bg-amber-50 text-amber-900 ring-amber-200', icon: AlertTriangle },
  success: { box: 'bg-green-50 text-green-800 ring-green-200', icon: CheckCircle2 },
  info: { box: 'bg-blue-50 text-blue-800 ring-blue-200', icon: Info },
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
      className={cn('flex gap-2.5 rounded-lg px-3.5 py-3 text-sm ring-1 ring-inset', box, className)}
    >
      <Icon aria-hidden className="mt-0.5 size-4 shrink-0" />
      <div className="min-w-0">{children}</div>
    </div>
  )
}
