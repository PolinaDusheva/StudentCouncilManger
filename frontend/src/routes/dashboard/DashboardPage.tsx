import { Alert } from '@/components/ui/Alert'
import { Card } from '@/components/ui/Card'
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
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">
          Здравей, {user.fullName.split(' ')[0]}!
        </h1>
        <p className="mt-1 text-sm text-muted">Влезе успешно. Функционалните модули предстоят.</p>
      </div>

      <Card>
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">Профил</h2>
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
      </Card>

      <Card>
        <h2 className="mb-1 font-serif text-[22px] leading-[1.3] font-normal text-ink">Права</h2>
        <p className="mb-4 text-sm text-muted">
          Изчислени от сървъра според ролята. Определят кои действия ще се показват в интерфейса.
        </p>

        <ul className="space-y-2 text-sm">
          {(Object.keys(PERMISSION_LABELS) as (keyof PermissionSet)[]).map((key) => (
            <li key={key} className="flex items-center gap-2">
              <span
                aria-hidden
                className={
                  permissions[key]
                    ? 'size-1.5 shrink-0 rounded-full bg-tone-success-text'
                    : 'size-1.5 shrink-0 rounded-full bg-border'
                }
              />
              <span className={permissions[key] ? 'text-ink' : 'text-faint'}>
                {PERMISSION_LABELS[key]}
              </span>
              <span className="sr-only">{permissions[key] ? '— разрешено' : '— забранено'}</span>
            </li>
          ))}
        </ul>
      </Card>

      <Alert tone="info">
        Следващи модули: членове и отдели, задачи, календар, дежурства, бюджет, известия.
      </Alert>
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted">{label}</dt>
      <dd className="mt-0.5 font-medium text-ink">{value}</dd>
    </div>
  )
}
