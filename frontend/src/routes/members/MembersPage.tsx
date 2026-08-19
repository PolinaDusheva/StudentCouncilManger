import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { UserPlus } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { EmptyState } from '@/components/ui/EmptyState'
import { Pagination } from '@/components/ui/Pagination'
import { Table, type Column } from '@/components/ui/Table'
import { getMembers, type MemberFilters as Filters } from '@/lib/api/members'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { usePermissions } from '@/lib/auth/useAuth'
import { useDebounce } from '@/lib/hooks/useDebounce'
import type { MemberSummaryDto } from '@/lib/types/dto'
import {
  DEPARTMENT_CODE_LABELS,
  MEMBER_STATUS_LABELS,
  SYSTEM_ROLE_LABELS,
  type MemberSort,
} from '@/lib/types/enums'

import { MemberFilters } from './MemberFilters'
import { roleTone, statusTone } from './memberLabels'

const PAGE_SIZE = 20

export function MembersPage() {
  const navigate = useNavigate()
  const { canManageMembers } = usePermissions()

  const [search, setSearch] = useState('')
  const [filters, setFilters] = useState<Filters>({ page: 1, pageSize: PAGE_SIZE, sort: 'fullName' })

  const debouncedSearch = useDebounce(search)

  // Typing narrows the result set, so a page number from the previous query is meaningless.
  useEffect(() => {
    setFilters((current) => ({ ...current, page: 1 }))
  }, [debouncedSearch])

  const query = { ...filters, search: debouncedSearch }

  const { data, isPending, isError, error } = useQuery({
    queryKey: queryKeys.members.list(query),
    queryFn: () => getMembers(query),
  })

  const applyFilters = (changes: Partial<Filters>) =>
    setFilters((current) => ({ ...current, ...changes, page: 1 }))

  const columns: Column<MemberSummaryDto>[] = [
    {
      key: 'name',
      header: 'Име',
      sortKey: 'fullName',
      render: (member) => (
        <div className="flex items-center gap-3">
          <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
          {/* A real link, so the row is reachable by keyboard — Table.onRowClick is mouse-only. */}
          <Link
            to={`/members/${member.id}`}
            className="hover:text-accent-hover font-medium hover:underline"
            onClick={(event) => event.stopPropagation()}
          >
            {member.fullName}
          </Link>
        </div>
      ),
    },
    {
      key: 'role',
      header: 'Роля',
      sortKey: 'role',
      render: (member) => <Badge tone={roleTone(member.role)}>{SYSTEM_ROLE_LABELS[member.role]}</Badge>,
    },
    {
      key: 'department',
      header: 'Отдел',
      render: (member) =>
        member.department ? (
          DEPARTMENT_CODE_LABELS[member.department]
        ) : (
          <span className="text-faint">Организационно ниво</span>
        ),
    },
    {
      key: 'status',
      header: 'Статус',
      render: (member) => (
        <Badge tone={statusTone(member.status)}>{MEMBER_STATUS_LABELS[member.status]}</Badge>
      ),
    },
  ]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="font-serif text-[34px] leading-[1.15] font-normal text-ink">Членове</h1>
          {data && (
            <p className="mt-1 text-sm text-muted">
              {data.totalCount} {data.totalCount === 1 ? 'член' : 'члена'}
            </p>
          )}
        </div>

        {canManageMembers && (
          <Button onClick={() => navigate('/members/new')}>
            <UserPlus aria-hidden className="size-4" />
            Добави член
          </Button>
        )}
      </div>

      <MemberFilters
        filters={filters}
        onChange={applyFilters}
        search={search}
        onSearchChange={setSearch}
      />

      {isError ? (
        <Alert tone="error">{errorMessage(error)}</Alert>
      ) : (
        <>
          <Table
            columns={columns}
            rows={data?.items ?? []}
            rowKey={(member) => member.id}
            loading={isPending}
            sort={filters.sort || undefined}
            onSortChange={(sort) => applyFilters({ sort: sort as MemberSort })}
            onRowClick={(member) => navigate(`/members/${member.id}`)}
            empty={
              <EmptyState
                title="Няма намерени членове"
                description="Опитай с други филтри или друго търсене."
              />
            }
          />

          {data && (
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              totalPages={data.totalPages}
              onPageChange={(page) => setFilters((current) => ({ ...current, page }))}
            />
          )}
        </>
      )}
    </div>
  )
}
