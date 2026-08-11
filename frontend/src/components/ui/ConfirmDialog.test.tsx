import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { ConfirmDialog } from './ConfirmDialog'

/**
 * Smoke coverage only. jsdom has no real `<dialog>` (see the stub in `src/test/setup.ts`),
 * so focus trapping, the backdrop and Esc dismissal are NOT verified here — those need a
 * manual check in a browser.
 */
const base = {
  open: true,
  title: 'Деактивиране на член',
  message: 'Членът няма да може да влиза в системата.',
}

describe('ConfirmDialog', () => {
  it('показва заглавието и съобщението', () => {
    render(<ConfirmDialog {...base} onConfirm={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByRole('heading', { name: base.title })).toBeInTheDocument()
    expect(screen.getByText(base.message)).toBeInTheDocument()
  })

  it('извиква onConfirm при потвърждаване', async () => {
    const onConfirm = vi.fn()
    render(<ConfirmDialog {...base} onConfirm={onConfirm} onCancel={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Потвърди' }))
    expect(onConfirm).toHaveBeenCalledOnce()
  })

  it('извиква onCancel при отказ', async () => {
    const onCancel = vi.fn()
    render(<ConfirmDialog {...base} onConfirm={vi.fn()} onCancel={onCancel} />)

    await userEvent.click(screen.getByRole('button', { name: 'Отказ' }))
    expect(onCancel).toHaveBeenCalledOnce()
  })

  it('заключва двата бутона, докато заявката тече', () => {
    render(<ConfirmDialog {...base} loading onConfirm={vi.fn()} onCancel={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'Отказ' })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Потвърди/ })).toBeDisabled()
  })

  it('позволява друг надпис на потвърждаващия бутон', () => {
    render(
      <ConfirmDialog {...base} confirmLabel="Деактивирай" onConfirm={vi.fn()} onCancel={vi.fn()} />,
    )

    expect(screen.getByRole('button', { name: 'Деактивирай' })).toBeInTheDocument()
  })
})
