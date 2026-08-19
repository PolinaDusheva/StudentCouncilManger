import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Bell, CheckCheck } from 'lucide-react'

import { Alert } from '@/components/ui/Alert'
import { Spinner } from '@/components/ui/Spinner'
import {
  getNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  notificationTarget,
} from '@/lib/api/notifications'
import { errorMessage } from '@/lib/api/problem'
import { queryKeys } from '@/lib/api/queryKeys'
import { useUnreadNotificationCount } from '@/lib/hooks/useUnreadNotificationCount'
import type { NotificationDto } from '@/lib/types/dto'
import { cn } from '@/lib/utils/cn'
import { formatDateTime } from '@/lib/utils/format'

import { NOTIFICATION_TYPE_LABELS } from './notificationLabels'

const PANEL_PAGE_SIZE = 10

/** Bell with an unread badge and a dropdown panel — the "page or panel" from the roadmap. */
export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const unreadCount = useUnreadNotificationCount()

  const list = useQuery({
    queryKey: queryKeys.notifications.list({ pageSize: PANEL_PAGE_SIZE }),
    queryFn: () => getNotifications({ pageSize: PANEL_PAGE_SIZE }),
    enabled: open,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all })

  const markRead = useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: invalidate,
  })

  const markAllRead = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: invalidate,
  })

  useEffect(() => {
    if (!open) return

    const handlePointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  const handleClick = (notification: NotificationDto) => {
    if (!notification.isRead) markRead.mutate(notification.id)

    const target = notificationTarget(notification)
    if (target) {
      setOpen(false)
      navigate(target)
    }
  }

  const count = unreadCount.data ?? 0

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={count > 0 ? `Известия, ${count} непрочетени` : 'Известия'}
        className="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
      >
        <Bell aria-hidden className="size-5" />
        {count > 0 && (
          <span
            aria-hidden
            className="absolute top-1 right-1 flex size-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-medium text-white"
          >
            {count > 9 ? '9+' : count}
          </span>
        )}
      </button>

      {open && (
        <div
          role="menu"
          aria-label="Известия"
          className="absolute right-0 z-10 mt-2 w-80 rounded-lg bg-white shadow-lg ring-1 ring-slate-200"
        >
          <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2">
            <span className="text-sm font-semibold text-slate-900">Известия</span>
            {count > 0 && (
              <button
                type="button"
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
                className="text-brand-600 hover:text-brand-700 inline-flex items-center gap-1 text-xs font-medium disabled:opacity-50"
              >
                <CheckCheck aria-hidden className="size-3.5" />
                Прочети всички
              </button>
            )}
          </div>

          <div className="max-h-96 overflow-y-auto">
            {list.isPending ? (
              <Spinner className="py-6" />
            ) : list.isError ? (
              <Alert tone="error" className="m-3">
                {errorMessage(list.error)}
              </Alert>
            ) : list.data.items.length === 0 ? (
              <p className="px-3 py-6 text-center text-sm text-slate-500">Няма известия.</p>
            ) : (
              <ul className="divide-y divide-slate-100">
                {list.data.items.map((notification) => (
                  <li key={notification.id}>
                    <NotificationRow notification={notification} onClick={handleClick} />
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

function NotificationRow({
  notification,
  onClick,
}: {
  notification: NotificationDto
  onClick: (notification: NotificationDto) => void
}) {
  const navigable = notificationTarget(notification) !== null

  return (
    <button
      type="button"
      role="menuitem"
      onClick={() => onClick(notification)}
      className={cn(
        'flex w-full gap-2.5 px-3 py-2.5 text-left hover:bg-slate-50',
        !navigable && 'cursor-default',
      )}
    >
      <span
        aria-hidden
        className={cn(
          'mt-1.5 size-1.5 shrink-0 rounded-full',
          notification.isRead ? 'bg-transparent' : 'bg-brand-600',
        )}
      />
      <span className="min-w-0 flex-1">
        <span className="block text-xs text-slate-500">
          {NOTIFICATION_TYPE_LABELS[notification.type] ?? notification.type}
        </span>
        <span
          className={cn(
            'block text-sm text-slate-900',
            notification.isRead ? 'font-normal' : 'font-medium',
          )}
        >
          {notification.title}
        </span>
        <span className="mt-0.5 block line-clamp-2 text-xs text-slate-600">{notification.body}</span>
        <span className="mt-0.5 block text-xs text-slate-400">
          {formatDateTime(notification.createdAtUtc)}
        </span>
      </span>
    </button>
  )
}
