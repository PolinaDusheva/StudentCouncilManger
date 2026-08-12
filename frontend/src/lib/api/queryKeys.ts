/**
 * Query keys for TanStack Query, kept in one place so invalidation after a mutation cannot
 * silently miss a cache entry because a key was spelled differently at the call site.
 *
 * Keys are hierarchical: invalidating `members.all` also invalidates every list and detail
 * entry beneath it.
 */

import type { MemberFilters } from './members'

export const queryKeys = {
  members: {
    all: ['members'] as const,
    lists: () => [...queryKeys.members.all, 'list'] as const,
    list: (filters: MemberFilters) => [...queryKeys.members.lists(), filters] as const,
    details: () => [...queryKeys.members.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.members.details(), id] as const,
    me: () => [...queryKeys.members.all, 'me'] as const,
  },
  departments: {
    all: ['departments'] as const,
    list: () => [...queryKeys.departments.all, 'list'] as const,
    detail: (id: string) => [...queryKeys.departments.all, 'detail', id] as const,
  },
} as const
