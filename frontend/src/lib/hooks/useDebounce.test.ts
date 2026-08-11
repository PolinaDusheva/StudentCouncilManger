import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { useDebounce } from './useDebounce'

beforeEach(() => vi.useFakeTimers())
afterEach(() => vi.useRealTimers())

describe('useDebounce', () => {
  it('връща началната стойност веднага', () => {
    const { result } = renderHook(() => useDebounce('старт', 300))
    expect(result.current).toBe('старт')
  })

  it('изчаква преди да пусне новата стойност', () => {
    const { result, rerender } = renderHook(({ value }) => useDebounce(value, 300), {
      initialProps: { value: 'а' },
    })

    rerender({ value: 'аб' })
    expect(result.current).toBe('а')

    act(() => void vi.advanceTimersByTime(300))
    expect(result.current).toBe('аб')
  })

  it('пуска само последната стойност при бързо писане', () => {
    const { result, rerender } = renderHook(({ value }) => useDebounce(value, 300), {
      initialProps: { value: 'а' },
    })

    rerender({ value: 'аб' })
    act(() => void vi.advanceTimersByTime(200))
    rerender({ value: 'абв' })
    act(() => void vi.advanceTimersByTime(200))

    // Още не са минали 300 мс от последната промяна.
    expect(result.current).toBe('а')

    act(() => void vi.advanceTimersByTime(100))
    expect(result.current).toBe('абв')
  })
})
