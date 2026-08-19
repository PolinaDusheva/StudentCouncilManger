import { NavLink, Outlet } from 'react-router-dom'
import { Building2, CalendarDays, ClipboardList, LayoutDashboard, LogOut, Users, Wallet } from 'lucide-react'

import { Avatar } from '@/components/ui/Avatar'
import { Button } from '@/components/ui/Button'
import { useAuth } from '@/lib/auth/useAuth'
import { cn } from '@/lib/utils/cn'
import { DEPARTMENT_CODE_LABELS, SYSTEM_ROLE_LABELS } from '@/lib/types/enums'
import { NotificationBell } from '@/routes/notifications/NotificationBell'

const NAV_ITEMS = [
  { to: '/', label: 'Табло', icon: LayoutDashboard, end: true },
  { to: '/tasks', label: 'Задачи', icon: ClipboardList, end: false },
  { to: '/events', label: 'Календар', icon: CalendarDays, end: false },
  { to: '/budget', label: 'Бюджет', icon: Wallet, end: false },
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
      <div className="gradient-accent-bar h-1.5" />

      <header className="border-b border-divider bg-surface">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4">
          <span className="gradient-accent-mark flex size-8 items-center justify-center rounded-xl">
            <Users aria-hidden className="size-4 text-white" />
          </span>
          <span className="hidden font-serif text-lg text-ink sm:inline">Студентски съвет</span>

          <div className="ml-auto flex items-center gap-2">
            <NotificationBell />

            <NavLink
              to="/profile"
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-page',
                  isActive && 'bg-page',
                )
              }
            >
              {/* `/auth/me` carries no photo URL, so the header always shows initials. */}
              {user && <Avatar photoUrl={null} fullName={user.fullName} size="sm" ring />}
              <span className="hidden text-right sm:block">
                <span className="block text-sm font-medium text-ink">{user?.fullName}</span>
                <span className="block text-xs text-muted">
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
                        ? 'border-accent text-ink'
                        : 'border-transparent text-muted hover:border-border hover:text-ink',
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
