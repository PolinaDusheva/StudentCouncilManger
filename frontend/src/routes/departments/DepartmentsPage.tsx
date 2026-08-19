import { Link } from 'react-router-dom'
import { Users } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Spinner } from '@/components/ui/Spinner'
import { errorMessage } from '@/lib/api/problem'
import { useDepartments } from '@/lib/hooks/useDepartments'
import type { DepartmentDto } from '@/lib/types/dto'

export function DepartmentsPage() {
  const { data: departments, isPending, isError, error } = useDepartments()

  if (isPending) return <Spinner />
  if (isError) return <Alert tone="error">{errorMessage(error)}</Alert>

  return (
    <div className="space-y-5">
      <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Отдели</h1>

      <div className="grid gap-4 sm:grid-cols-2">
        {departments.map((department) => (
          <DepartmentCard key={department.id} department={department} />
        ))}
      </div>
    </div>
  )
}

function DepartmentCard({ department }: { department: DepartmentDto }) {
  const { president, vicePresident, secretary } = department.leadership
  const lead = president ?? vicePresident ?? secretary

  return (
    <Link
      to={`/departments/${department.id}`}
      className="block rounded-[15px] bg-surface p-5 shadow-[0_4px_15px_rgba(0,0,0,0.05)] ring-1 ring-divider transition-shadow hover:shadow-md hover:ring-accent"
    >
      <h2 className="font-semibold text-ink">{department.name}</h2>

      {department.description && (
        <p className="mt-1 line-clamp-2 text-sm text-muted">{department.description}</p>
      )}

      <div className="mt-4 flex items-center gap-4 text-sm text-muted">
        <span className="inline-flex items-center gap-1.5">
          <Users aria-hidden className="size-4" />
          {department.memberCount} {department.memberCount === 1 ? 'член' : 'члена'}
        </span>

        {lead && <span className="truncate">Ръководи: {lead.fullName}</span>}
      </div>
    </Link>
  )
}
