import { describe, expect, it } from 'vitest'

import { formatFileSize } from './download'

describe('formatFileSize', () => {
  it('показва байтове без преобразуване', () => {
    expect(formatFileSize(0)).toBe('0 B')
    expect(formatFileSize(512)).toBe('512 B')
  })

  it('преминава към килобайти', () => {
    expect(formatFileSize(1024)).toBe('1,0 KB')
    expect(formatFileSize(2048)).toBe('2,0 KB')
  })

  it('преминава към мегабайти', () => {
    expect(formatFileSize(1024 * 1024)).toBe('1,0 MB')
    expect(formatFileSize(Math.round(2.5 * 1024 * 1024))).toBe('2,5 MB')
  })

  it('маха дробната част при големи стойности', () => {
    expect(formatFileSize(15 * 1024 * 1024)).toBe('15 MB')
  })

  it('спира на гигабайти', () => {
    expect(formatFileSize(3 * 1024 ** 3)).toBe('3,0 GB')
  })
})
