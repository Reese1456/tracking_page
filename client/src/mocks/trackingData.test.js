import { describe, expect, it } from 'vitest'
import { shipments } from './trackingData.js'

// The mock data is typed by hand, so these catch a slip that would make it
// disagree with the rules the API follows (docs/api-contract.md).
describe('mock shipments', () => {
  it.each(shipments)('$waybill is consistent with its newest event', (shipment) => {
    const times = shipment.events.map((event) => Date.parse(event.timestamp))
    const newest = shipment.events[0]

    expect(times).toEqual([...times].sort((a, b) => b - a))
    expect(shipment.status).toBe(newest.status)
    expect(shipment.lastUpdated).toBe(newest.timestamp)
    expect(shipment.isException).toBe(shipment.status === 'Exception')
    expect(shipment.exceptionReason !== null).toBe(shipment.isException)
    expect(Object.keys(shipment.receiver)).toEqual(['name', 'city'])
  })
})
