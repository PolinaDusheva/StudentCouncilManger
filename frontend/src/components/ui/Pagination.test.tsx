import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Pagination } from './Pagination'

const base = { page: 2, pageSize: 20, totalCount: 95, totalPages: 5 }

describe('Pagination', () => {
  it('показва обхвата на текущата страница', () => {
    render(<Pagination {...base} onPageChange={vi.fn()} />)
    expect(screen.getByText(/21\s*–\s*40 от 95/)).toBeInTheDocument()
  })

  it('изчислява по-къс обхват за последната непълна страница', () => {
    render(<Pagination {...base} page={5} onPageChange={vi.fn()} />)
    expect(screen.getByText(/81\s*–\s*95 от 95/)).toBeInTheDocument()
  })

  it('спира „назад“ на първата страница', () => {
    render(<Pagination {...base} page={1} onPageChange={vi.fn()} />)
    expect(screen.getByRole('button', { name: /предишна/i })).toBeDisabled()
    expect(screen.getByRole('button', { name: /следваща/i })).toBeEnabled()
  })

  it('спира „напред“ на последната страница', () => {
    render(<Pagination {...base} page={5} onPageChange={vi.fn()} />)
    expect(screen.getByRole('button', { name: /следваща/i })).toBeDisabled()
  })

  it('подава следващия номер на страница', async () => {
    const onPageChange = vi.fn()
    render(<Pagination {...base} onPageChange={onPageChange} />)

    await userEvent.click(screen.getByRole('button', { name: /следваща/i }))
    expect(onPageChange).toHaveBeenCalledWith(3)
  })

  it('подава предишния номер на страница', async () => {
    const onPageChange = vi.fn()
    render(<Pagination {...base} onPageChange={onPageChange} />)

    await userEvent.click(screen.getByRole('button', { name: /предишна/i }))
    expect(onPageChange).toHaveBeenCalledWith(1)
  })

  it('не се показва при една страница', () => {
    const { container } = render(
      <Pagination page={1} pageSize={20} totalCount={7} totalPages={1} onPageChange={vi.fn()} />,
    )
    expect(container).toBeEmptyDOMElement()
  })
})
