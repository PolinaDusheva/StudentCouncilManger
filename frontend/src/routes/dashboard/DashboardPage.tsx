import { Alert } from '@/components/ui/Alert'
import { useAuth, usePermissions } from '@/lib/auth/useAuth'
import { DEPARTMENT_CODE_LABELS, MEMBER_STATUS_LABELS, SYSTEM_ROLE_LABELS } from '@/lib/types/enums'
import type { PermissionSet } from '@/lib/types/dto'

/** Bulgarian labels for the permission flags returned by `GET /auth/me`. */
const PERMISSION_LABELS: Record<keyof PermissionSet, string> = {
  canManageMembers: 'Управление на членове',
  canManageBudget: 'Управление на бюджет',
  canManageDuties: 'Управление на дежурства',
  canCreateOrgTask: 'Създаване на организационни задачи',
  canCreateDeptTask: 'Създаване на задачи на отдел',
  canManageEvents: 'Управление на събития',
}

/**
 * Placeholder home screen for the auth milestone: it renders what the session actually
 * carries, which is the quickest way to confirm the token flow works end to end.
 * The functional modules replace this.
 */
export function DashboardPage() {
  const { user } = useAuth()
  const permissions = usePermissions()

  if (!user) return null

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-slate-900">Здравей, {user.fullName.split(' ')[0]}!</h1>
        <p className="mt-1 text-sm text-slate-600">
          Влезе успешно. Функционалните модули предстоят.
        </p>
      </div>

      <section className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-4 text-sm font-semibold text-slate-900">Профил</h2>
        <dl className="grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
          <Field label="Имейл" value={user.email} />
          <Field label="Телефон" value={user.phoneNumber ?? '—'} />
          <Field label="Роля" value={SYSTEM_ROLE_LABELS[user.role]} />
          <Field
            label="Отдел"
            value={
              user.department
                ? (user.departmentName ?? DEPARTMENT_CODE_LABELS[user.department])
                : 'Организационно ниво'
            }
          />
          <Field label="Статус" value={MEMBER_STATUS_LABELS[user.status]} />
        </dl>
      </section>

      <section className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
        <h2 className="mb-1 text-sm font-semibold text-slate-900">Права</h2>
        <p className="mb-4 text-sm text-slate-600">
          Изчислени от сървъра според ролята. Определят кои действия ще се показват в интерфейса.
        </p>

        <ul className="space-y-2 text-sm">
          {(Object.keys(PERMISSION_LABELS) as (keyof PermissionSet)[]).map((key) => (
            <li key={key} className="flex items-center gap-2">
              <span
                aria-hidden
                className={
                  permissions[key]
                    ? 'size-1.5 shrink-0 rounded-full bg-green-600'
                    : 'size-1.5 shrink-0 rounded-full bg-slate-300'
                }
              />
              <span className={permissions[key] ? 'text-slate-900' : 'text-slate-400'}>
                {PERMISSION_LABELS[key]}
              </span>
              <span className="sr-only">{permissions[key] ? '— разрешено' : '— забранено'}</span>
            </li>
          ))}
        </ul>
      </section>

      <Alert tone="info">
        Следващи модули: членове и отдели, задачи, календар, дежурства, бюджет, известия.
      </Alert>
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{value}</dd>
    </div>
  )
}
