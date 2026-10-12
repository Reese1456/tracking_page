import axios from 'axios'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { api, getDemoWaybills, getTracking } from './trackingClient.js'

describe('trackingClient against the real API', () => {
  const originalAdapter = api.defaults.adapter
  let requests

  // Answers in place of the network and records what Axios would have sent.
  function respondWith(data) {
    api.defaults.adapter = async (config) => {
      requests.push(config)
      return { data, status: 200, statusText: 'OK', headers: {}, config }
    }
  }

  beforeEach(() => {
    vi.stubEnv('VITE_USE_MOCK', 'false')
    requests = []
  })
  afterEach(() => {
    api.defaults.adapter = originalAdapter
  })

  it('requests the encoded waybill from the configured API and returns the body', async () => {
    respondWith({ waybill: 'WB10000004' })
    const { signal } = new AbortController()

    const data = await getTracking('WB/1 ?', signal)

    expect(data).toEqual({ waybill: 'WB10000004' })
    expect(requests).toHaveLength(1)
    expect(requests[0]).toMatchObject({
      method: 'get',
      baseURL: 'http://api.test',
      url: '/api/tracking/WB%2F1%20%3F',
      timeout: 30_000,
      signal,
    })
  })

  it('requests the demo waybills and returns the body', async () => {
    respondWith(['WB10000001'])

    const data = await getDemoWaybills()

    expect(data).toEqual(['WB10000001'])
    expect(requests[0]).toMatchObject({ method: 'get', url: '/api/tracking' })
  })
})

describe('trackingClient in mock mode', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  // Attaches a handler straight away, so a rejection while the clock is
  // being advanced isn't reported as unhandled.
  function settle(promise) {
    return promise.then(
      (value) => ({ value }),
      (error) => ({ error }),
    )
  }

  it('answers only after the 400 ms delay', async () => {
    let answered = false
    getTracking('WB10000004').then(() => {
      answered = true
    })

    await vi.advanceTimersByTimeAsync(399)
    expect(answered).toBe(false)
    await vi.advanceTimersByTimeAsync(1)
    expect(answered).toBe(true)
  })

  it('trims and uppercases the waybill, and returns a fresh copy each time', async () => {
    const first = settle(getTracking(' wb10000004 '))
    await vi.advanceTimersByTimeAsync(400)
    const { value: shipment } = await first
    expect(shipment.waybill).toBe('WB10000004')

    shipment.receiver.name = 'Changed by a caller'
    const second = settle(getTracking('WB10000004'))
    await vi.advanceTimersByTimeAsync(400)

    expect((await second).value.receiver.name).toBe('T. Mokoena')
  })

  it.each([
    ['hello', 400, {
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
      title: 'Invalid waybill format',
      status: 400,
      detail: 'A waybill is WB followed by 8 digits, for example WB10000004.',
    }],
    ['wb99999999', 404, {
      type: 'https://tools.ietf.org/html/rfc9110#section-15.5.5',
      title: 'Shipment not found',
      status: 404,
      detail: 'No shipment was found for waybill WB99999999.',
    }],
  ])('rejects %s like the API: %i with its ProblemDetails body', async (waybill, status, body) => {
    const outcome = settle(getTracking(waybill))
    await vi.advanceTimersByTimeAsync(400)

    const { error } = await outcome
    expect(axios.isAxiosError(error)).toBe(true)
    expect(error.response.status).toBe(status)
    expect(error.response.data).toEqual(body)
    expect(error.response.headers.get('content-type')).toMatch(/^application\/problem\+json/)
    expect(error.config.url).toBe(`/api/tracking/${waybill}`)
  })

  it('returns the demo waybills sorted', async () => {
    const outcome = settle(getDemoWaybills())
    await vi.advanceTimersByTimeAsync(400)

    expect((await outcome).value).toEqual([
      'WB10000001', 'WB10000002', 'WB10000003', 'WB10000004', 'WB10000005', 'WB10000006',
    ])
  })

  it('rejects with a CanceledError when aborted during the delay', async () => {
    const controller = new AbortController()
    const outcome = settle(getTracking('WB10000004', controller.signal))

    controller.abort()

    expect(axios.isCancel((await outcome).error)).toBe(true)
  })

  it('rejects at once when the signal is already aborted', async () => {
    const controller = new AbortController()
    controller.abort()

    const { error } = await settle(getTracking('WB10000004', controller.signal))

    expect(axios.isCancel(error)).toBe(true)
  })
})
