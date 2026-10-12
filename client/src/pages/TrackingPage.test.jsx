import { act, fireEvent, screen, within } from '@testing-library/react'
import { AxiosError } from 'axios'
import { useNavigate } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../App.jsx'
import { getTracking } from '../api/trackingClient.js'
import { shipments } from '../mocks/trackingData.js'
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

function wait(ms) {
  return act(() => vi.advanceTimersByTimeAsync(ms))
}

// The error Axios throws when the server answers with this status and body.
function responseError(status, data) {
  return new AxiosError(`Request failed with status code ${status}`, AxiosError.ERR_BAD_RESPONSE, undefined, undefined, {
    status,
    data,
  })
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

  it('says the server is waking up once a lookup takes 3 seconds, then shows the shipment', async () => {
    let answer
    vi.mocked(getTracking).mockReturnValueOnce(new Promise((resolve) => (answer = resolve)))
    renderAt('/track/WB10000004')

    await wait(2999)
    expect(screen.getByRole('status')).not.toHaveTextContent('waking up')
    await wait(1)
    expect(screen.getByRole('status')).toHaveTextContent(
      'Looking up WB10000004… The server has been asleep and is waking up. This can take up to a minute.',
    )

    await act(async () => answer(structuredClone(shipments.find((s) => s.waybill === 'WB10000004'))))
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
    expect(screen.getByText('T. Mokoena')).toBeInTheDocument()
  })

  it('starts the 3-second wait again for a newly searched waybill', async () => {
    vi.mocked(getTracking)
      .mockReturnValueOnce(new Promise(() => {}))
      .mockReturnValueOnce(new Promise(() => {}))
    renderAt('/track/WB10000004')
    await wait(2000)

    fireEvent.change(screen.getByRole('textbox', { name: 'Waybill number' }), { target: { value: 'WB10000005' } })
    fireEvent.click(screen.getByRole('button', { name: 'Track' }))
    await wait(2000)

    expect(screen.getByRole('status')).toHaveTextContent('Looking up WB10000005…')
    expect(screen.getByRole('status')).not.toHaveTextContent('waking up')
    await wait(1000)
    expect(screen.getByRole('status')).toHaveTextContent('waking up')
  })

  it('shows dates in South African time', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    expect(screen.getByText('09 Oct 2026')).toBeInTheDocument()
    // lastUpdated and the newest event are both 05:42 UTC.
    expect(screen.getAllByText('08 Oct 2026, 07:42')).toHaveLength(2)
  })

  // The cards are description lists, so each value is announced with its
  // label. The status uses the stepper's words, not the raw enum.
  it('lists the shipment and receiver details as labelled values', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    const labels = screen.getAllByRole('term').map((term) => term.textContent)
    const values = screen.getAllByRole('definition').map((definition) => definition.textContent)
    expect(labels).toEqual(['Waybill', 'Status', 'Estimated delivery', 'Last updated', 'Name', 'City'])
    expect(values).toEqual([
      'WB10000004',
      'Out for Delivery',
      '09 Oct 2026',
      '08 Oct 2026, 07:42',
      'T. Mokoena',
      'Johannesburg',
    ])
  })

  it('lists every event newest first, with machine-readable times', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    const items = historyItems()
    expect(items.map((item) => within(item).getByText(/^Parcel|^Arrived|^Departed/).textContent)).toEqual([
      'Parcel out for delivery',
      'Arrived at regional hub',
      'Departed sorting hub',
      'Parcel received at sorting hub',
      'Parcel packed by sender',
    ])
    expect(items[0]).toHaveTextContent('Midrand Depot')
    expect(within(items[0]).getByText('08 Oct 2026, 07:42')).toHaveAttribute('datetime', '2026-10-08T05:42:00+00:00')
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
    expect(screen.getByRole('region', { name: /Shipment details/ })).toContainElement(screen.getByText('T. Mokoena'))
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

    expect(screen.getByRole('alert')).toHaveTextContent("Something went wrongWe couldn't load this shipment just now.")
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(screen.getByRole('status')).toBeInTheDocument()
    await finishRequests()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.getByText('T. Mokoena')).toBeInTheDocument()
  })

  it("shows the API's title and detail for a server error, with a retry button", async () => {
    vi.mocked(getTracking).mockRejectedValueOnce(
      responseError(503, { title: 'Service Unavailable', status: 503, detail: 'The database is not answering.' }),
    )
    renderAt('/track/WB10000004')
    await finishRequests()

    expect(screen.getByRole('alert')).toHaveTextContent('Service UnavailableThe database is not answering.')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  // Render's proxy answers with an HTML page if the API can't be reached.
  it('falls back to the generic message when the error body is not ProblemDetails', async () => {
    vi.mocked(getTracking).mockRejectedValueOnce(responseError(502, '<html><body>Bad Gateway</body></html>'))
    renderAt('/track/WB10000004')
    await finishRequests()

    expect(screen.getByRole('alert')).toHaveTextContent("Something went wrongWe couldn't load this shipment just now.")
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
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

  it('checks the format in the search box here too, without demo chips', async () => {
    renderAt('/track/WB10000004')
    await finishRequests()

    fireEvent.change(screen.getByRole('textbox', { name: 'Waybill number' }), { target: { value: 'hello' } })
    fireEvent.submit(screen.getByRole('search'))

    expect(screen.getByText(/WB followed by 8 digits/)).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Waybill WB10000004')
    expect(screen.queryByText('Try a demo waybill:')).not.toBeInTheDocument()
  })

  // Only the search box checks the format. A URL typed by hand goes to the
  // API as it is, which normalises it or answers 400.
  it('sends a hand-typed URL to the API unchanged', async () => {
    renderAt('/track/wb10000004')
    await finishRequests()

    expect(getTracking).toHaveBeenCalledWith('wb10000004', expect.any(AbortSignal))
    expect(screen.getByText('T. Mokoena')).toBeInTheDocument()
  })

  // Asking again would get the same 400, so there's nothing to retry.
  it("shows the API's 400 message for a badly formatted URL, without a retry button", async () => {
    renderAt('/track/hello')
    await finishRequests()

    expect(getTracking).toHaveBeenCalledWith('hello', expect.any(AbortSignal))
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Invalid waybill formatA waybill is WB followed by 8 digits, for example WB10000004.',
    )
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
  })

  it('sends an unknown path back to the search page', () => {
    renderAt('/somewhere/else')

    expect(screen.getByRole('heading', { level: 1, name: 'Track your parcel' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toContainElement(screen.getByRole('search'))
  })
})
