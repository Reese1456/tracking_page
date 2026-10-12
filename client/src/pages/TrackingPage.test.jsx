import { act, fireEvent, screen, within } from '@testing-library/react'
import { AxiosError } from 'axios'
import { useNavigate } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../App.jsx'
import { getTracking } from '../api/trackingClient.js'
import { renderWithProviders, setScreenWidth } from '../test/render.jsx'
import { theme } from '../theme.js'

// The real mock client still answers; spying lets one test make a request fail.
vi.mock('../api/trackingClient.js', { spy: true })

const DESKTOP_WIDTH = 1280
const MOBILE_WIDTH = 375

function renderAt(route) {
  renderWithProviders(<App />, { route })
}

function finishRequests() {
  return act(() => vi.advanceTimersByTimeAsync(400))
}

// The browser's Back button, which changes the URL without touching the page.
function BackButton() {
  const navigate = useNavigate()
  return <button onClick={() => navigate(-1)}>Back</button>
}

function historyItems() {
  return within(screen.getByRole('list', { name: 'Tracking history' })).getAllByRole('listitem')
}

describe('TrackingPage', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    setScreenWidth(DESKTOP_WIDTH)
  })
  afterEach(() => vi.useRealTimers())

  it('shows a loading message, then the shipment', async () => {
    renderAt('/track/WB10000004')

    expect(screen.getByRole('status')).toHaveTextContent('Looking up WB10000004…')
    await finishRequests()

    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Waybill WB10000004' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Shipment' })).toBeInTheDocument()
    expect(screen.getByText('T. Mokoena')).toBeInTheDocument()
    expect(screen.getByText('Johannesburg')).toBeInTheDocument()
  })

  it('shows dates in South African time', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    expect(screen.getByText('09 Oct 2026')).toBeInTheDocument()
    // lastUpdated and the newest event are both 05:42 UTC.
    expect(screen.getAllByText('08 Oct 2026, 07:42')).toHaveLength(2)
  })

  it('shows the status chip with the stepper label', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    const status = screen.getByText('Status').nextElementSibling
    expect(status).toHaveTextContent('Out for Delivery')
  })

  it('lists every event newest first', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    const items = historyItems()
    expect(items).toHaveLength(5)
    expect(items[0]).toHaveTextContent('Parcel out for delivery')
    expect(items[0]).toHaveTextContent('Midrand Depot')
    expect(items[4]).toHaveTextContent('Parcel packed by sender')
  })

  it('highlights the exception event in the history', async () => {
    renderAt('/track/WB10000006')
    await finishRequests()

    const [exceptionEvent, ...otherEvents] = historyItems()
    expect(within(exceptionEvent).getByRole('img', { name: 'Exception' })).toBeInTheDocument()
    expect(exceptionEvent).toHaveStyle({ borderLeftColor: theme.palette.error.main })
    for (const event of otherEvents) {
      expect(within(event).queryByRole('img', { name: 'Exception' })).not.toBeInTheDocument()
      expect(event).not.toHaveStyle({ borderLeftColor: theme.palette.error.main })
    }
  })

  it('shows the shipment and receiver cards side by side on desktop', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    expect(screen.queryByRole('button', { name: /Shipment details/ })).not.toBeInTheDocument()
    expect(screen.getByText('T. Mokoena')).toBeVisible()
  })

  it('puts the shipment and receiver cards in a collapsed accordion on mobile', async () => {
    setScreenWidth(MOBILE_WIDTH)
    renderAt('/track/WB10000004')
    await finishRequests()

    const toggle = screen.getByRole('button', { name: /Shipment details/ })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    expect(toggle).toHaveTextContent('Est. delivery 09 Oct 2026')
    expect(screen.getByText('T. Mokoena')).not.toBeVisible()

    fireEvent.click(toggle)
    // Not a request this time: lets the expand animation finish.
    await finishRequests()

    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('T. Mokoena')).toBeVisible()
  })

  it('shows a not-found message for an unknown waybill', async () => {
    renderAt('/track/WB99999999')
    await finishRequests()

    expect(screen.getByRole('alert')).toHaveTextContent("We couldn't find that shipment")
    expect(screen.getByRole('alert')).toHaveTextContent('WB99999999')
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
  })

  it('shows an error with a retry button that loads the shipment', async () => {
    vi.mocked(getTracking).mockRejectedValueOnce(new AxiosError('Network Error', AxiosError.ERR_NETWORK))
    renderAt('/track/WB10000004')
    await finishRequests()

    expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong')
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(screen.getByRole('status')).toBeInTheDocument()
    await finishRequests()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.getByText('T. Mokoena')).toBeInTheDocument()
  })

  it('loads the new shipment when another waybill is searched, and the old one on Back', async () => {
    renderWithProviders(
      <>
        <App />
        <BackButton />
      </>,
      { route: '/track/WB10000004' },
    )
    await finishRequests()
    const box = () => screen.getByRole('textbox', { name: 'Waybill number' })
    expect(box()).toHaveValue('WB10000004')

    fireEvent.change(box(), { target: { value: 'WB10000005' } })
    fireEvent.click(screen.getByRole('button', { name: 'Track' }))
    await finishRequests()

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Waybill WB10000005')
    expect(screen.getByText('A. Botha')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Back' }))
    await finishRequests()

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Waybill WB10000004')
    expect(box()).toHaveValue('WB10000004')
  })

  it('sends an unknown path back to the search page', () => {
    renderAt('/somewhere/else')

    expect(screen.getByRole('heading', { level: 1, name: 'Track your parcel' })).toBeInTheDocument()
  })
})
