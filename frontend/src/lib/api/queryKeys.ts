/**
 * Query keys for TanStack Query, kept in one place so invalidation after a mutation cannot
 * silently miss a cache entry because a key was spelled differently at the call site.
 *
 * Keys are hierarchical: invalidating `members.all` also invalidates every list and detail
 * entry beneath it.
 */

import type { EventFilters } from './events'
import type { MemberFilters } from './members'
import type { TaskFilters } from './tasks'

export const queryKeys = {
  events: {
    all: ['events'] as const,
    lists: () => [...queryKeys.events.all, 'list'] as const,
    list: (filters: EventFilters) => [...queryKeys.events.lists(), filters] as const,
    detail: (id: string) => [...queryKeys.events.all, 'detail', id] as const,
  },
  tasks: {
    all: ['tasks'] as const,
    lists: () => [...queryKeys.tasks.all, 'list'] as const,
    list: (filters: TaskFilters) => [...queryKeys.tasks.lists(), filters] as const,
    /** No filters: `GET /tasks/mine` takes no parameters. */
    mine: () => [...queryKeys.tasks.all, 'mine'] as const,
    board: () => [...queryKeys.tasks.all, 'board'] as const,
    details: () => [...queryKeys.tasks.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.tasks.details(), id] as const,
    comments: (id: string) => [...queryKeys.tasks.detail(id), 'comments'] as const,
    documents: (id: string) => [...queryKeys.tasks.detail(id), 'documents'] as const,
  },
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
