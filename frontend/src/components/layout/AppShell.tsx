import { NavLink, Outlet } from 'react-router-dom'
import { Building2, LayoutDashboard, LogOut, Users } from 'lucide-react'

import { Avatar } from '@/components/ui/Avatar'
import { Button } from '@/components/ui/Button'
import { useAuth } from '@/lib/auth/useAuth'
import { cn } from '@/lib/utils/cn'
import { DEPARTMENT_CODE_LABELS, SYSTEM_ROLE_LABELS } from '@/lib/types/enums'

const NAV_ITEMS = [
  { to: '/', label: 'Табло', icon: LayoutDashboard, end: true },
  { to: '/members', label: 'Членове', icon: Users, end: false },
  { to: '/departments', label: 'Отдели', icon: Building2, end: false },
]

/** Chrome for the signed-in area: header, primary navigation and the routed content. */
export function AppShell() {
  const { user, signOut } = useAuth()

  const roleLabel = user ? SYSTEM_ROLE_LABELS[user.role] : ''
  const departmentLabel = user?.department ? DEPARTMENT_CODE_LABELS[user.department] : null

  return (
    <div className="min-h-dvh">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4">
          <span className="bg-brand-600 flex size-8 items-center justify-center rounded-lg">
            <Users aria-hidden className="size-4 text-white" />
          </span>
          <span className="hidden font-semibold text-slate-900 sm:inline">Студентски съвет</span>

          <div className="ml-auto flex items-center gap-2">
            <NavLink
              to="/profile"
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-slate-100',
                  isActive && 'bg-slate-100',
                )
              }
            >
              {/* `/auth/me` carries no photo URL, so the header always shows initials. */}
              {user && <Avatar photoUrl={null} fullName={user.fullName} size="sm" />}
              <span className="hidden text-right sm:block">
                <span className="block text-sm font-medium text-slate-900">{user?.fullName}</span>
                <span className="block text-xs text-slate-500">
                  {roleLabel}
                  {departmentLabel && ` · ${departmentLabel}`}
                </span>
              </span>
            </NavLink>

            <Button variant="secondary" size="sm" onClick={() => void signOut()}>
              <LogOut aria-hidden className="size-4" />
              <span className="hidden sm:inline">Изход</span>
            </Button>
          </div>
        </div>

        <nav aria-label="Основна навигация" className="mx-auto max-w-6xl px-4">
          <ul className="-mb-px flex gap-1">
            {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-2 border-b-2 px-3 py-2.5 text-sm font-medium transition-colors',
                      isActive
                        ? 'border-brand-600 text-brand-700'
                        : 'border-transparent text-slate-600 hover:border-slate-300 hover:text-slate-900',
                    )
                  }
                >
                  <Icon aria-hidden className="size-4" />
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
