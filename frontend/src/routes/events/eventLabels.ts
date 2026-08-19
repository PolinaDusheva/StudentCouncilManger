/** Bulgarian labels and tones for the calendar enums. */

import type { BadgeTone } from '@/components/ui/Badge'
import type { EventType, RecurrenceType } from '@/lib/types/enums'

export const EVENT_TYPE_LABELS: Record<EventType, string> = {
  Meeting: 'Среща',
  PublicEvent: 'Публично събитие',
  InternalMeeting: 'Вътрешна среща',
  SportsEvent: 'Спортно събитие',
  Deadline: 'Краен срок',
  Other: 'Друго',
}

export const EVENT_TYPE_TONES: Record<EventType, BadgeTone> = {
  Meeting: 'info',
  PublicEvent: 'success',
  InternalMeeting: 'neutral',
  SportsEvent: 'warning',
  Deadline: 'danger',
  Other: 'neutral',
}

export const RECURRENCE_LABELS: Record<RecurrenceType, string> = {
  None: 'Без повторение',
  Weekly: 'Всяка седмица',
  Monthly: 'Всеки месец',
}

/** Dot colour used on the month grid, matching the badge tone of each type. */
export const EVENT_TYPE_DOTS: Record<EventType, string> = {
  Meeting: 'bg-data',
  PublicEvent: 'bg-tone-success-text',
  InternalMeeting: 'bg-faint',
  SportsEvent: 'bg-tone-warning-text',
  Deadline: 'bg-danger',
  Other: 'bg-faint',
}
