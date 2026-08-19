import { useQuery } from '@tanstack/react-query'

import { getNotifications } from '@/lib/api/notifications'
import { queryKeys } from '@/lib/api/queryKeys'

/**
 * Unread count for the bell badge.
 *
 * There is no dedicated count endpoint, so this asks for one row (`unreadOnly=true,
 * pageSize=1`) and reads `totalCount` off the pagination envelope — cheaper than fetching
 * full notification rows just to count them.
 *
 * Polled every 30s: the backend has no WebSocket/SSE channel, so this is the only way the
 * badge reflects notifications created by someone else's action.
 */
export function useUnreadNotificationCount() {
  return useQuery({
    queryKey: queryKeys.notifications.unreadCount(),
    queryFn: () => getNotifications({ unreadOnly: true, pageSize: 1 }).then((page) => page.totalCount),
    refetchInterval: 30_000,
    // A stale badge for a few seconds is fine; refetching on every window focus is not worth
    // the extra requests for a number that changes this rarely.
    staleTime: 15_000,
  })
}
