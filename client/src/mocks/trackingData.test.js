import { describe, expect, it } from 'vitest'
import { shipments } from './trackingData.js'

const UTC_TIMESTAMP = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\+00:00$/
const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/

const SHIPMENT_FIELDS = [
  'waybill', 'status', 'stepIndex', 'isException', 'exceptionReason',
  'receiver', 'estimatedDelivery', 'lastUpdated', 'events',
]
const EVENT_FIELDS = ['status', 'description', 'location', 'timestamp']

// The mock data is typed by hand, so these catch a slip that would make it
// disagree with docs/api-contract.md. Claude compared all six with the real
// API's responses when they were written; these keep them honest after that.
describe('mock shipments', () => {
  it('are the six demo waybills, one per status, with the API step index', () => {
    // Written out rather than calculated: the client never maps status to step.
    expect(shipments.map((s) => [s.waybill, s.status, s.stepIndex])).toEqual([
      ['WB10000001', 'Packed', 0],
      ['WB10000002', 'Processing', 1],
      ['WB10000003', 'InTransit', 2],
      ['WB10000004', 'OutForDelivery', 3],
      ['WB10000005', 'Delivered', 4],
      ['WB10000006', 'Exception', 4],
    ])
  })

  it.each(shipments)('$waybill has exactly the contract fields, in the contract formats', (shipment) => {
    expect(Object.keys(shipment).sort()).toEqual([...SHIPMENT_FIELDS].sort())
    expect(Object.keys(shipment.receiver).sort()).toEqual(['city', 'name'])
    expect(shipment.estimatedDelivery).toMatch(DATE_ONLY)
    expect(shipment.lastUpdated).toMatch(UTC_TIMESTAMP)
    for (const event of shipment.events) {
      expect(Object.keys(event).sort()).toEqual([...EVENT_FIELDS].sort())
      expect(event.timestamp).toMatch(UTC_TIMESTAMP)
    }
  })

  it.each(shipments)('$waybill is consistent with its newest event', (shipment) => {
    const times = shipment.events.map((event) => Date.parse(event.timestamp))
    const newest = shipment.events[0]

    expect(times).toEqual([...times].sort((a, b) => b - a))
    expect(shipment.status).toBe(newest.status)
    expect(shipment.lastUpdated).toBe(newest.timestamp)
    expect(shipment.isException).toBe(shipment.status === 'Exception')
    if (shipment.isException) {
      expect(shipment.exceptionReason).toEqual(expect.stringMatching(/\S/))
    } else {
      expect(shipment.exceptionReason).toBeNull()
    }
  })

  it('WB10000004 matches the example in docs/api-contract.md', () => {
    const shipment = shipments.find((s) => s.waybill === 'WB10000004')

    expect(shipment).toMatchObject({
      status: 'OutForDelivery',
      stepIndex: 3,
      isException: false,
      exceptionReason: null,
      receiver: { name: 'T. Mokoena', city: 'Johannesburg' },
      estimatedDelivery: '2026-10-09',
      lastUpdated: '2026-10-08T05:42:00+00:00',
    })
    expect(shipment.events[0]).toEqual({
      status: 'OutForDelivery',
      description: 'Parcel out for delivery',
      location: 'Midrand Depot',
      timestamp: '2026-10-08T05:42:00+00:00',
    })
  })
})
