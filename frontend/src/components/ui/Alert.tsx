import type { ReactNode } from 'react'
import { AlertCircle, CheckCircle2, Info } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

type Tone = 'error' | 'success' | 'info'

const TONES: Record<Tone, { box: string; icon: typeof Info }> = {
  error: { box: 'bg-red-50 text-red-800 ring-red-200', icon: AlertCircle },
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
