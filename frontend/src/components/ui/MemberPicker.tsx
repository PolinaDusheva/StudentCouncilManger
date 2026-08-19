import { useEffect, useId, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Check, Search, X } from 'lucide-react'

import { getMembers } from '@/lib/api/members'
import { queryKeys } from '@/lib/api/queryKeys'
import { useDebounce } from '@/lib/hooks/useDebounce'
import type { MemberSummaryDto } from '@/lib/types/dto'
import { SYSTEM_ROLE_LABELS } from '@/lib/types/enums'
import { cn } from '@/lib/utils/cn'

import { Avatar } from './Avatar'
import { Spinner } from './Spinner'

interface MemberPickerProps {
  label: string
  /** Selected member ids. */
  value: string[]
  onChange: (ids: string[]) => void
  /**
   * Members already known to the caller (e.g. a task's current assignees), so their chips can
   * show a name without waiting for a search that may never include them.
   */
  knownMembers?: MemberSummaryDto[]
  error?: string
  hint?: string
}

/**
 * Multi-select over the member directory: type to search, click to add or remove.
 *
 * Only active members are offered — `GET /members` defaults to active-only, which matches the
 * server rule that every assignee must be an existing, active member.
 */
export function MemberPicker({
  label,
  value,
  onChange,
  knownMembers = [],
  error,
  hint,
}: MemberPickerProps) {
  const id = useId()
  const errorId = `${id}-error`
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounce(search)

  const filters = { search: debouncedSearch, pageSize: 10 as const }

  const { data, isFetching } = useQuery({
    queryKey: queryKeys.members.list(filters),
    queryFn: () => getMembers(filters),
  })

  /**
   * Names for the chips. A selected member disappears from the search results as soon as the
   * query changes, so every member ever seen is remembered rather than looked up again.
   */
  const [seen, setSeen] = useState<Record<string, MemberSummaryDto>>({})

  useEffect(() => {
    if (!data?.items.length) return
    setSeen((current) => {
      const next = { ...current }
      for (const member of data.items) next[member.id] = member
      return next
    })
  }, [data])

  // `knownMembers` is merged at render rather than stored, so no effect has to track an array
  // identity the caller re-creates on every render.
  const selected = useMemo(() => {
    const byId = new Map(knownMembers.map((member) => [member.id, member]))
    for (const [memberId, member] of Object.entries(seen)) byId.set(memberId, member)

    return value.map((memberId) => byId.get(memberId)).filter((member) => member !== undefined)
  }, [value, seen, knownMembers])

  const toggle = (memberId: string) =>
    onChange(value.includes(memberId) ? value.filter((v) => v !== memberId) : [...value, memberId])

  return (
    <div className="space-y-1.5">
      <span className="block text-sm font-semibold text-ink-soft">{label}</span>

      {selected.length > 0 && (
        <ul className="flex flex-wrap gap-1.5 pb-1">
          {selected.map((member) => (
            <li key={member.id}>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-page py-1 pr-1 pl-2 text-sm">
                {member.fullName}
                <button
                  type="button"
                  onClick={() => toggle(member.id)}
                  aria-label={`Премахни ${member.fullName}`}
                  className="rounded-full p-0.5 text-faint hover:bg-divider hover:text-ink-soft"
                >
                  <X aria-hidden className="size-3.5" />
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}

      <div className="relative">
        <Search aria-hidden className="absolute top-2.5 left-3 size-4 text-faint" />
        <input
          id={id}
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Търси по име или имейл"
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className={cn(
            'block w-full rounded-xl border-2 border-line bg-surface py-2 pr-3 pl-9 text-sm text-ink',
            'shadow-[inset_0_2px_5px_rgba(0,0,0,0.05)] placeholder:text-faint',
            'focus:border-accent focus:shadow-[0_0_0_4px_rgba(255,60,112,0.15)] focus:outline-none',
            error && 'border-danger',
          )}
        />
      </div>

      <div className="max-h-56 overflow-y-auto rounded-lg ring-1 ring-divider">
        {isFetching && !data ? (
          <Spinner className="py-4" />
        ) : data?.items.length === 0 ? (
          <p className="px-3 py-4 text-center text-sm text-muted">Няма намерени членове.</p>
        ) : (
          <ul className="divide-y divide-divider">
            {data?.items.map((member) => {
              const isSelected = value.includes(member.id)

              return (
                <li key={member.id}>
                  <button
                    type="button"
                    onClick={() => toggle(member.id)}
                    aria-pressed={isSelected}
                    className={cn(
                      'flex w-full items-center gap-2.5 px-3 py-2 text-left text-sm hover:bg-row-hover',
                      isSelected && 'bg-accent-soft hover:bg-accent-soft',
                    )}
                  >
                    <Avatar photoUrl={member.photoUrl} fullName={member.fullName} size="sm" />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-medium text-ink">
                        {member.fullName}
                      </span>
                      <span className="block truncate text-xs text-muted">
                        {SYSTEM_ROLE_LABELS[member.role]}
                      </span>
                    </span>
                    {isSelected && <Check aria-hidden className="text-accent size-4 shrink-0" />}
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>

      {error ? (
        <p id={errorId} role="alert" className="text-sm text-danger">
          {error}
        </p>
      ) : hint ? (
        <p className="text-sm text-muted">{hint}</p>
      ) : null}
    </div>
  )
}
