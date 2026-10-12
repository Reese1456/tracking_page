import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { shipments } from '../mocks/trackingData.js'
import { renderWithProviders, setScreenWidth } from '../test/render.jsx'
import { theme } from '../theme.js'
import DeliveryStepper from './DeliveryStepper.jsx'

const DESKTOP_WIDTH = 1280
const MOBILE_WIDTH = 375

function shipmentWithStatus(status) {
  return shipments.find((shipment) => shipment.status === status)
}

function renderStepper(shipment) {
  renderWithProviders(<DeliveryStepper shipment={shipment} />)
  return screen.getAllByRole('listitem')
}

// The accessible name of each step's icon, in step order.
function stepStates(steps) {
  return steps.map((step) => within(step).getByRole('img').textContent)
}

describe('DeliveryStepper', () => {
  beforeEach(() => setScreenWidth(DESKTOP_WIDTH))

  it.each([
    ['Packed', 0, 'Order Packed', ['Current step', 'Pending', 'Pending', 'Pending', 'Pending']],
    ['Processing', 1, 'Processing', ['Completed', 'Current step', 'Pending', 'Pending', 'Pending']],
    ['InTransit', 2, 'In Transit', ['Completed', 'Completed', 'Current step', 'Pending', 'Pending']],
    ['OutForDelivery', 3, 'Out for Delivery', ['Completed', 'Completed', 'Completed', 'Current step', 'Pending']],
  ])('marks the right step as current for %s', (status, stepIndex, label, expectedStates) => {
    const steps = renderStepper(shipmentWithStatus(status))

    expect(steps).toHaveLength(5)
    expect(steps[stepIndex]).toHaveAttribute('aria-current', 'step')
    expect(steps[stepIndex]).toHaveTextContent(label)
    expect(steps.filter((step) => step.hasAttribute('aria-current'))).toHaveLength(1)
    expect(stepStates(steps)).toEqual(expectedStates)
  })

  // The status says In Transit but the API says step 1. The stepper must
  // follow the API, because the step mapping lives only in the backend.
  it("follows the API's stepIndex even when it disagrees with the status", () => {
    const steps = renderStepper({ ...shipmentWithStatus('InTransit'), stepIndex: 1 })

    expect(steps[1]).toHaveAttribute('aria-current', 'step')
    expect(stepStates(steps)).toEqual(['Completed', 'Current step', 'Pending', 'Pending', 'Pending'])
  })

  it('draws completed steps as green checks, the current step in blue and pending steps in grey', () => {
    const steps = renderStepper(shipmentWithStatus('InTransit'))
    const icon = (index) => within(steps[index]).getByRole('img')

    expect(icon(0)).toHaveAttribute('data-testid', 'CheckCircleIcon')
    expect(icon(0)).toHaveStyle({ color: theme.palette.success.main })
    expect(icon(2)).toHaveAttribute('data-testid', 'LocalShippingIcon')
    expect(icon(2)).toHaveStyle({ color: theme.palette.primary.main })
    expect(icon(3)).toHaveAttribute('data-testid', 'DeliveryDiningIcon')
    expect(icon(3)).toHaveStyle({ color: theme.palette.pending.main })
    expect(within(steps[2]).getByText('In Transit')).toHaveStyle({ fontWeight: 500 })
    expect(within(steps[3]).getByText('Out for Delivery')).not.toHaveStyle({ fontWeight: 500 })
  })

  it('shows every step as completed for Delivered', () => {
    const steps = renderStepper(shipmentWithStatus('Delivered'))

    expect(stepStates(steps)).toEqual(['Completed', 'Completed', 'Completed', 'Completed', 'Completed'])
    expect(steps[4]).toHaveTextContent('Delivered')
    expect(steps[4]).toHaveAttribute('aria-current', 'step')
  })

  it('shows Delivery Exception with the reason in the error state', () => {
    const steps = renderStepper(shipmentWithStatus('Exception'))

    const finalStep = steps[4]
    expect(finalStep).toHaveAttribute('aria-current', 'step')
    expect(within(finalStep).getByRole('img', { name: 'Exception' })).toHaveStyle({ color: theme.palette.error.main })
    expect(within(finalStep).getByText('Recipient not available')).toBeInTheDocument()
    expect(within(finalStep).getByText('Delivery Exception')).toHaveStyle({ color: theme.palette.error.main })
    expect(screen.queryByText('Delivered')).not.toBeInTheDocument()
    expect(stepStates(steps.slice(0, 4))).toEqual(['Completed', 'Completed', 'Completed', 'Completed'])
  })

  it('does not use the error colour for a normal final step', () => {
    const steps = renderStepper(shipmentWithStatus('Delivered'))

    expect(within(steps[4]).getByText('Delivered')).not.toHaveStyle({ color: theme.palette.error.main })
  })

  // MUI's documented class names are the only sign of orientation: the
  // stepper has no role or attribute that says which way it runs.
  it('renders vertically when matchMedia reports a mobile width', () => {
    setScreenWidth(MOBILE_WIDTH)

    renderStepper(shipmentWithStatus('InTransit'))

    expect(screen.getByRole('list')).toHaveClass('MuiStepper-vertical')
  })

  it('renders horizontally on a desktop width', () => {
    renderStepper(shipmentWithStatus('InTransit'))

    expect(screen.getByRole('list')).toHaveClass('MuiStepper-horizontal')
  })

  // md starts at exactly 900px.
  it.each([
    [899, 'MuiStepper-vertical'],
    [900, 'MuiStepper-horizontal'],
  ])('at %ipx renders with %s', (width, orientationClass) => {
    setScreenWidth(width)

    renderStepper(shipmentWithStatus('InTransit'))

    expect(screen.getByRole('list')).toHaveClass(orientationClass)
  })
})
