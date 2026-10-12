import axios, { AxiosError, AxiosHeaders, CanceledError } from 'axios'
import { normalizeWaybill, WAYBILL_PATTERN } from '../constants/waybill.js'
import { shipments } from '../mocks/trackingData.js'

const MOCK_DELAY_MS = 400

const PROBLEM_TYPES = {
  400: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
  404: 'https://tools.ietf.org/html/rfc9110#section-15.5.5',
}

// Exported so tests can swap in a network-free adapter. A cold API on
// Render's free tier can take this long to wake up; Part 9 adds a message
// for the wait.
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 30_000,
})

// Read on each call rather than once at load, so tests can switch modes.
function isMockMode() {
  return import.meta.env.VITE_USE_MOCK === 'true'
}

export async function getTracking(waybill, signal) {
  const url = `/api/tracking/${encodeURIComponent(waybill)}`
  if (isMockMode()) {
    return mockGetTracking(waybill, url, signal)
  }
  const response = await api.get(url, { signal })
  return response.data
}

export async function getDemoWaybills(signal) {
  if (isMockMode()) {
    await mockDelay(signal)
    return shipments.map((shipment) => shipment.waybill).sort()
  }
  const response = await api.get('/api/tracking', { signal })
  return response.data
}

async function mockGetTracking(waybill, url, signal) {
  await mockDelay(signal)

  const normalized = normalizeWaybill(waybill)
  if (!WAYBILL_PATTERN.test(normalized)) {
    throw problem(url, 400, 'Invalid waybill format', 'A waybill is WB followed by 8 digits, for example WB10000004.')
  }

  const shipment = shipments.find((s) => s.waybill === normalized)
  if (!shipment) {
    throw problem(url, 404, 'Shipment not found', `No shipment was found for waybill ${normalized}.`)
  }

  // The real API sends a fresh object every time, so a caller that changes
  // one can't affect the next response.
  return structuredClone(shipment)
}

// Rejects with Axios's own CanceledError on abort, so callers handle a
// cancelled mock request exactly like a cancelled real one.
function mockDelay(signal) {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(new CanceledError())
      return
    }

    const onAbort = () => {
      clearTimeout(timer)
      reject(new CanceledError())
    }
    const timer = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort)
      resolve()
    }, MOCK_DELAY_MS)
    signal?.addEventListener('abort', onAbort, { once: true })
  })
}

// Builds the error Axios throws for a 4xx response, with the API's
// ProblemDetails body, so error handling can't tell mock from real.
function problem(url, status, title, detail) {
  const config = { method: 'get', baseURL: api.defaults.baseURL, url, headers: new AxiosHeaders() }
  const response = {
    status,
    statusText: '',
    headers: new AxiosHeaders({ 'content-type': 'application/problem+json; charset=utf-8' }),
    config,
    data: { type: PROBLEM_TYPES[status], title, status, detail },
  }
  return new AxiosError(
    `Request failed with status code ${status}`,
    AxiosError.ERR_BAD_REQUEST,
    config,
    undefined,
    response,
  )
}
