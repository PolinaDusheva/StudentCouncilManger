import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Pencil } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Spinner } from '@/components/ui/Spinner'
import { deactivateMember, getMember, reactivateMember } from '@/lib/api/members'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { useAuth, usePermissions } from '@/lib/auth/useAuth'
import { formatDate } from '@/lib/utils/format'
import { DEPARTMENT_CODE_LABELS, MEMBER_STATUS_LABELS, SYSTEM_ROLE_LABELS } from '@/lib/types/enums'

import { roleTone, statusTone } from './memberLabels'

export function MemberDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const { canManageMembers } = usePermissions()

  const [confirmOpen, setConfirmOpen] = useState(false)

  const { data: member, isPending, isError, error } = useQuery({
    queryKey: queryKeys.members.detail(id),
    queryFn: () => getMember(id),
    enabled: id !== '',
  })

  const toggleStatus = useMutation({
    mutationFn: () =>
      member?.status === 'Active' ? deactivateMember(id) : reactivateMember(id),
    onSuccess: async () => {
      setConfirmOpen(false)
      // Both the detail and every list page now show a stale status.
      await queryClient.invalidateQueries({ queryKey: queryKeys.members.all })
    },
  })

  if (isPending) return <Spinner />
  if (isError) return <Alert tone="error">{errorMessage(error)}</Alert>

  const isActive = member.status === 'Active'
  // Deactivating yourself would revoke your own tokens mid-session.
  const isSelf = member.id === user?.id

  return (
    <div className="space-y-5">
      <Link
        to="/members"
        className="inline-flex items-center gap-1.5 text-sm text-slate-600 hover:text-slate-900"
      >
        <ArrowLeft aria-hidden className="size-4" />
        Всички членове
      </Link>

      <div className="rounded-xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-start gap-5">
          <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="lg" />

          <div className="min-w-0 flex-1">
            <h1 className="text-xl font-semibold text-slate-900">{member.fullName}</h1>
            <div className="mt-2 flex flex-wrap gap-2">
              <Badge tone={roleTone(member.role)}>{SYSTEM_ROLE_LABELS[member.role]}</Badge>
              <Badge tone={statusTone(member.status)}>{MEMBER_STATUS_LABELS[member.status]}</Badge>
            </div>
          </div>

          {canManageMembers && (
            <div className="flex gap-2">
              <Button variant="secondary" onClick={() => navigate(`/members/${member.id}/edit`)}>
                <Pencil aria-hidden className="size-4" />
                Редактирай
              </Button>

              {!isSelf && (
                <Button
                  variant={isActive ? 'danger' : 'primary'}
                  onClick={() => setConfirmOpen(true)}
                >
                  {isActive ? 'Деактивирай' : 'Активирай'}
                </Button>
              )}
            </div>
          )}
        </div>

        <dl className="mt-6 grid gap-x-6 gap-y-4 border-t border-slate-200 pt-6 text-sm sm:grid-cols-2">
          <Field label="Имейл" value={member.email} />
          <Field label="Телефон" value={member.phoneNumber ?? '—'} />
          <Field
            label="Отдел"
            value={
              member.department
                ? (member.departmentName ?? DEPARTMENT_CODE_LABELS[member.department])
                : 'Организационно ниво'
            }
          />
          <Field label="Присъединен на" value={formatDate(member.joinedOn)} />
        </dl>
      </div>

      {toggleStatus.isError && <Alert tone="error">{errorMessage(toggleStatus.error)}</Alert>}

      <ConfirmDialog
        open={confirmOpen}
        title={isActive ? 'Деактивиране на член' : 'Активиране на член'}
        message={
          isActive
            ? `${member.fullName} няма да може да влиза в системата. Активните сесии се прекратяват веднага.`
            : `${member.fullName} ще може отново да влиза в системата.`
        }
        confirmLabel={isActive ? 'Деактивирай' : 'Активирай'}
        tone={isActive ? 'danger' : 'primary'}
        loading={toggleStatus.isPending}
        onConfirm={() => toggleStatus.mutate()}
        onCancel={() => setConfirmOpen(false)}
      />
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
