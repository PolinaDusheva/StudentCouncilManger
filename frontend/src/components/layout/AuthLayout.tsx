import type { ReactNode } from 'react'
import { Users } from 'lucide-react'

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
          <span className="bg-brand-600 mb-4 flex size-11 items-center justify-center rounded-xl">
            <Users aria-hidden className="size-6 text-white" />
          </span>
          <h1 className="text-xl font-semibold text-slate-900">{title}</h1>
          {subtitle && <p className="mt-1.5 text-sm text-slate-600">{subtitle}</p>}
        </div>

        <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">{children}</div>

        {footer && <div className="mt-5 text-center text-sm text-slate-600">{footer}</div>}
      </div>
    </div>
  )
}
