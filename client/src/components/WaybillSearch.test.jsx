import { act, fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Route, Routes, useLocation } from 'react-router'
import { renderWithProviders } from '../test/render.jsx'
import WaybillSearch from './WaybillSearch.jsx'

function CurrentPath() {
  return <p data-testid="path">{useLocation().pathname}</p>
}

// Renders the search at / and a stand-in tracking page that shows the URL it
// was sent to.
function renderSearch() {
  renderWithProviders(
    <Routes>
      <Route path="/" element={<WaybillSearch />} />
      <Route path="/track/:waybill" element={<CurrentPath />} />
    </Routes>,
  )
}

function search(text) {
  fireEvent.change(screen.getByRole('textbox', { name: 'Waybill number' }), { target: { value: text } })
  fireEvent.click(screen.getByRole('button', { name: 'Track' }))
}

describe('WaybillSearch', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('navigates to the normalised waybill on submit', () => {
    renderSearch()

    search('  wb10000004 ')

    expect(screen.getByTestId('path')).toHaveTextContent('/track/WB10000004')
  })

  it.each([['hello'], ['WB123'], ['']])('shows a format hint and stays put for %j', (text) => {
    renderSearch()

    search(text)

    expect(screen.getByText(/WB followed by 8 digits/)).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'Waybill number' })).toHaveAttribute('aria-invalid', 'true')
    expect(screen.queryByTestId('path')).not.toBeInTheDocument()
  })

  it('clears the hint when the waybill is edited', () => {
    renderSearch()
    search('hello')

    fireEvent.change(screen.getByRole('textbox', { name: 'Waybill number' }), { target: { value: 'WB1' } })

    expect(screen.queryByText(/WB followed by 8 digits/)).not.toBeInTheDocument()
  })

  it('navigates when a demo waybill chip is clicked', async () => {
    renderSearch()
    await act(() => vi.advanceTimersByTimeAsync(400))

    expect(screen.getAllByRole('button', { name: /^WB\d{8}$/ })).toHaveLength(6)
    fireEvent.click(screen.getByRole('button', { name: 'WB10000006' }))

    expect(screen.getByTestId('path')).toHaveTextContent('/track/WB10000006')
  })
})
