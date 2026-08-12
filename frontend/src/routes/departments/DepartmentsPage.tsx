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
      <h1 className="text-2xl font-semibold text-slate-900">Отдели</h1>

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
      className="hover:ring-brand-300 block rounded-xl bg-white p-5 shadow-sm ring-1 ring-slate-200 transition-shadow hover:shadow-md"
    >
      <h2 className="font-semibold text-slate-900">{department.name}</h2>

      {department.description && (
        <p className="mt-1 line-clamp-2 text-sm text-slate-600">{department.description}</p>
      )}

      <div className="mt-4 flex items-center gap-4 text-sm text-slate-500">
        <span className="inline-flex items-center gap-1.5">
          <Users aria-hidden className="size-4" />
          {department.memberCount} {department.memberCount === 1 ? 'член' : 'члена'}
        </span>

        {lead && <span className="truncate">Ръководи: {lead.fullName}</span>}
      </div>
    </Link>
  )
}
