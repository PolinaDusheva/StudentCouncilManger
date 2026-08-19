import type { ReactNode } from 'react'
import { Users } from 'lucide-react'

import { Card } from '@/components/ui/Card'

interface AuthLayoutProps {
  title: string
  subtitle?: ReactNode
  children: ReactNode
  /** Secondary links rendered under the card (e.g. "back to sign in"). */
  footer?: ReactNode
}

/** Centred card used by every unauthenticated screen. */
export function AuthLayout({ title, subtitle, children, footer }: AuthLayoutProps) {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex flex-col items-center text-center">
          <span className="gradient-accent-mark mb-4 flex size-11 items-center justify-center rounded-2xl">
            <Users aria-hidden className="size-6 text-white" />
          </span>
          <h1 className="font-serif text-xl font-normal text-ink">{title}</h1>
          {subtitle && <p className="mt-1.5 text-sm text-muted">{subtitle}</p>}
        </div>

        <Card variant="panel">{children}</Card>

        {footer && <div className="mt-5 text-center text-sm text-muted">{footer}</div>}
      </div>
    </div>
  )
}
