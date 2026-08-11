import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import { Table, type Column } from './Table'

interface Row {
  id: string
  name: string
  joinedOn: string
}

const rows: Row[] = [
  { id: '1', name: 'Иван Петров', joinedOn: '2026-01-15' },
  { id: '2', name: 'Мария Георгиева', joinedOn: '2026-02-20' },
]

const columns: Column<Row>[] = [
  { key: 'name', header: 'Име', render: (row) => row.name, sortKey: 'fullName' },
  { key: 'joinedOn', header: 'Присъединен', render: (row) => row.joinedOn, sortKey: 'joinedOn' },
]

const rowKey = (row: Row) => row.id

describe('Table', () => {
  it('показва заглавия и редове', () => {
    render(<Table columns={columns} rows={rows} rowKey={rowKey} />)

    expect(screen.getByRole('columnheader', { name: /Име/ })).toBeInTheDocument()
    expect(screen.getByText('Иван Петров')).toBeInTheDocument()
    expect(screen.getByText('Мария Георгиева')).toBeInTheDocument()
  })

  it('показва индикатор за зареждане вместо редове', () => {
    render(<Table columns={columns} rows={[]} rowKey={rowKey} loading />)

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('показва празно състояние при липса на редове', () => {
    render(<Table columns={columns} rows={[]} rowKey={rowKey} />)

    expect(screen.getByText('Няма записи')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('сортира нова колона възходящо', async () => {
    const onSortChange = vi.fn()
    render(<Table columns={columns} rows={rows} rowKey={rowKey} onSortChange={onSortChange} />)

    await userEvent.click(screen.getByRole('button', { name: /Присъединен/ }))
    expect(onSortChange).toHaveBeenCalledWith('joinedOn')
  })

  it('обръща посоката при повторен клик по същата колона', async () => {
    const onSortChange = vi.fn()
    render(
      <Table
        columns={columns}
        rows={rows}
        rowKey={rowKey}
        sort="joinedOn"
        onSortChange={onSortChange}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: /Присъединен/ }))
    expect(onSortChange).toHaveBeenCalledWith('-joinedOn')
  })

  it('връща се към възходящо от низходящо', async () => {
    const onSortChange = vi.fn()
    render(
      <Table
        columns={columns}
        rows={rows}
        rowKey={rowKey}
        sort="-joinedOn"
        onSortChange={onSortChange}
      />,
    )

    await userEvent.click(screen.getByRole('button', { name: /Присъединен/ }))
    expect(onSortChange).toHaveBeenCalledWith('joinedOn')
  })

  it('обявява посоката на сортиране пред екранните четци', () => {
    const { rerender } = render(
      <Table columns={columns} rows={rows} rowKey={rowKey} sort="joinedOn" onSortChange={vi.fn()} />,
    )
    expect(screen.getByRole('columnheader', { name: /Присъединен/ })).toHaveAttribute(
      'aria-sort',
      'ascending',
    )

    rerender(
      <Table
        columns={columns}
        rows={rows}
        rowKey={rowKey}
        sort="-joinedOn"
        onSortChange={vi.fn()}
      />,
    )
    expect(screen.getByRole('columnheader', { name: /Присъединен/ })).toHaveAttribute(
      'aria-sort',
      'descending',
    )
  })

  it('не прави заглавието бутон без обработчик за сортиране', () => {
    render(<Table columns={columns} rows={rows} rowKey={rowKey} />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('подава реда при клик', async () => {
    const onRowClick = vi.fn()
    render(<Table columns={columns} rows={rows} rowKey={rowKey} onRowClick={onRowClick} />)

    await userEvent.click(screen.getByText('Иван Петров'))
    expect(onRowClick).toHaveBeenCalledWith(rows[0])
  })
})
