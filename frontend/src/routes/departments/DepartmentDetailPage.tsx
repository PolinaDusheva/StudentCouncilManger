import { Link, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Card } from '@/components/ui/Card'
import { EmptyState } from '@/components/ui/EmptyState'
import { Spinner } from '@/components/ui/Spinner'
import { getDepartment } from '@/lib/api/departments'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import type { MemberSummaryDto } from '@/lib/types/dto'
import { SYSTEM_ROLE_LABELS } from '@/lib/types/enums'

import { roleTone } from '@/routes/members/memberLabels'

export function DepartmentDetailPage() {
  const { id = '' } = useParams()

  const { data: department, isPending, isError, error } = useQuery({
    queryKey: queryKeys.departments.detail(id),
    queryFn: () => getDepartment(id),
    enabled: id !== '',
  })

  if (isPending) return <Spinner />
  if (isError) return <Alert tone="error">{errorMessage(error)}</Alert>

  const { president, vicePresident, secretary } = department.leadership
  const leadership = [
    { title: 'Председател', member: president },
    { title: 'Зам.-председател', member: vicePresident },
    { title: 'Секретар', member: secretary },
  ].filter((entry) => entry.member)

  return (
    <div className="space-y-5">
      <Link
        to="/departments"
        className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-ink"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Всички отдели
      </Link>

      <div>
        <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">{department.name}</h1>
        {department.description && <p className="mt-1 text-sm text-muted">{department.description}</p>}
      </div>

      {leadership.length > 0 && (
        <Card>
          <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">Ръководство</h2>
          <ul className="grid gap-4 sm:grid-cols-3">
            {leadership.map(({ title, member }) => (
              <li key={title}>
                <p className="mb-2 text-xs text-muted">{title}</p>
                <MemberLine member={member!} />
              </li>
            ))}
          </ul>
        </Card>
      )}

      <Card>
        <h2 className="mb-4 font-serif text-[22px] leading-[1.3] font-normal text-ink">
          Състав ({department.memberCount})
        </h2>

        {department.members.length === 0 ? (
          <EmptyState title="Няма членове в този отдел" />
        ) : (
          <ul className="divide-y divide-divider">
            {department.members.map((member) => (
              <li key={member.id} className="flex items-center justify-between gap-3 py-3">
                <MemberLine member={member} />
                <Badge tone={roleTone(member.role)}>{SYSTEM_ROLE_LABELS[member.role]}</Badge>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  )
}

function MemberLine({ member }: { member: MemberSummaryDto }) {
  return (
    <Link to={`/members/${member.id}`} className="group flex min-w-0 items-center gap-2.5">
      <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
      <span className="group-hover:text-accent-hover truncate text-sm font-medium text-ink group-hover:underline">
        {member.fullName}
      </span>
    </Link>
  )
}
