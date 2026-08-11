import { useEffect, useState } from 'react'

/**
 * Delays propagating a rapidly changing value — for search boxes, where every keystroke
 * would otherwise fire a request.
 */
export function useDebounce<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
