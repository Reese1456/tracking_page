import { act, fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { Route, Routes, useLocation } from 'react-router'
import { getDemoWaybills } from '../api/trackingClient.js'
import { renderWithProviders } from '../test/render.jsx'
import WaybillSearch from './WaybillSearch.jsx'

vi.mock('../api/trackingClient.js', { spy: true })

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

  it('navigates when the form is submitted, as pressing Enter does', () => {
    renderSearch()
    fireEvent.change(screen.getByRole('textbox', { name: 'Waybill number' }), { target: { value: 'WB10000003' } })

    fireEvent.submit(screen.getByRole('search'))

    expect(screen.getByTestId('path')).toHaveTextContent('/track/WB10000003')
  })

  it('does not check the format when the box is only clicked', () => {
    renderSearch()

    fireEvent.click(screen.getByRole('textbox', { name: 'Waybill number' }))

    expect(screen.queryByText(/WB followed by 8 digits/)).not.toBeInTheDocument()
  })

  it.each([['hello'], ['WB123'], [''], ['WB10000004xyz'], ['WB100000045']])('shows a format hint and stays put for %j', (text) => {
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

  it('shows no demo section when there are no demo waybills', async () => {
    vi.mocked(getDemoWaybills).mockResolvedValueOnce([])
    renderSearch()
    await act(() => vi.advanceTimersByTimeAsync(400))

    expect(screen.queryByText('Try a demo waybill:')).not.toBeInTheDocument()
  })
})
