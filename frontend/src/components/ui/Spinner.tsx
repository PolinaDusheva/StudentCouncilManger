import { Loader2 } from 'lucide-react'

import { cn } from '@/lib/utils/cn'

interface SpinnerProps {
  label?: string
  className?: string
}

/** Centred loading indicator for route-level and panel-level waits. */
export function Spinner({ label = 'Зареждане…', className }: SpinnerProps) {
  return (
    <div role="status" className={cn('flex items-center justify-center gap-2 py-8 text-slate-500', className)}>
      <Loader2 aria-hidden className="size-5 animate-spin" />
      <span className="text-sm">{label}</span>
    </div>
  )
}
