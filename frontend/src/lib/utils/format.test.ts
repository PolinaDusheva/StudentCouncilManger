import { describe, expect, it } from 'vitest'

import { formatDate, formatDateTime } from './format'

// Българският локал добавя суфикса „г.“ — това е коректният формат за езика,
// затова се приема както е, вместо да се реже след форматирането.
describe('formatDate', () => {
  it('форматира DateOnly от API-то', () => {
    expect(formatDate('2026-03-09')).toBe('09.03.2026 г.')
  })

  it('форматира и ISO дата с час', () => {
    expect(formatDate('2026-03-09T14:30:00Z')).toBe('09.03.2026 г.')
  })

  it('връща тире при липсваща стойност', () => {
    expect(formatDate(null)).toBe('—')
    expect(formatDate(undefined)).toBe('—')
    expect(formatDate('')).toBe('—')
  })

  it('връща тире вместо "Invalid Date" при боклук', () => {
    expect(formatDate('не-е-дата')).toBe('—')
  })
})

describe('formatDateTime', () => {
  it('показва дата и час', () => {
    // API-то връща UTC; изходът е в местната зона на браузъра.
    expect(formatDateTime('2026-03-09T14:30:00Z')).toMatch(/^09\.03\.2026.*\d{2}:\d{2}$/)
  })

  it('връща тире при липсваща стойност', () => {
    expect(formatDateTime(null)).toBe('—')
  })
})
