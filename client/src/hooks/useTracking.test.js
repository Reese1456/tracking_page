import { act, renderHook } from '@testing-library/react'
import { AxiosError } from 'axios'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getTracking } from '../api/trackingClient.js'
import { useTracking } from './useTracking.js'

// The real mock client still runs; spying records each call's signal and
// lets a test replace a single response.
vi.mock('../api/trackingClient.js', { spy: true })

function renderTracking(waybill) {
  return renderHook((props) => useTracking(props.waybill), { initialProps: { waybill } })
}

// Fast-forwards past the mock client's 400 ms delay instead of waiting for
// it, so a slow test machine can't make a test time out.
function finishRequests() {
  return act(() => vi.advanceTimersByTimeAsync(400))
}

function serverError(status) {
  return new AxiosError(`Request failed with status code ${status}`, AxiosError.ERR_BAD_RESPONSE, undefined, undefined, {
    status,
    data: { title: 'An error occurred while processing your request.', status },
  })
}

describe('useTracking', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('is idle when there is no waybill', () => {
    const { result } = renderTracking('')

    expect(result.current.state).toBe('idle')
    expect(getTracking).not.toHaveBeenCalled()
  })

  it('reaches success with the shipment for a known waybill', async () => {
    const { result } = renderTracking('WB10000004')
    expect(result.current.state).toBe('loading')

    await finishRequests()

    expect(result.current.state).toBe('success')
    expect(result.current.data.waybill).toBe('WB10000004')
    expect(result.current.data.stepIndex).toBe(3)
  })

  it('reaches notFound for an unknown waybill', async () => {
    const { result } = renderTracking('WB99999999')

    await finishRequests()

    expect(result.current.state).toBe('notFound')
    expect(result.current.data).toBeNull()
    expect(result.current.error.response.status).toBe(404)
  })

  it('reaches error for a badly formatted waybill', async () => {
    const { result } = renderTracking('hello')

    await finishRequests()

    expect(result.current.state).toBe('error')
    expect(result.current.error.response.data.title).toBe('Invalid waybill format')
  })

  it.each([
    ['a server error', serverError(500)],
    ['a network failure', new AxiosError('Network Error', AxiosError.ERR_NETWORK)],
    ['a timeout', new AxiosError('timeout of 30000ms exceeded', AxiosError.ECONNABORTED)],
  ])('reaches error, not notFound, for %s', async (_, failure) => {
    vi.mocked(getTracking).mockRejectedValueOnce(failure)
    const { result } = renderTracking('WB10000004')

    await finishRequests()

    expect(result.current.state).toBe('error')
    expect(result.current.error).toBe(failure)
  })

  it('shows loading, not the previous shipment, as soon as the waybill changes', async () => {
    const { result, rerender } = renderTracking('WB10000001')
    await finishRequests()

    rerender({ waybill: 'WB10000002' })

    expect(result.current.state).toBe('loading')
    expect(result.current.data).toBeNull()
    await finishRequests()
    expect(result.current.data.waybill).toBe('WB10000002')
  })

  it('shows loading when returning to an earlier waybill', async () => {
    const { result, rerender } = renderTracking('WB10000001')
    await finishRequests()

    rerender({ waybill: 'WB10000002' })
    rerender({ waybill: 'WB10000001' })

    expect(result.current.state).toBe('loading')
    await finishRequests()
    expect(result.current.data.waybill).toBe('WB10000001')
  })

  it('aborts the previous request when the waybill changes mid-request', async () => {
    const { result, rerender } = renderTracking('WB10000001')

    rerender({ waybill: 'WB10000002' })

    const firstSignal = vi.mocked(getTracking).mock.calls[0][1]
    expect(firstSignal.aborted).toBe(true)
    await finishRequests()
    expect(result.current.data.waybill).toBe('WB10000002')
  })

  it.each([
    ['succeeds', (request) => request.resolve({ waybill: 'WB10000001' })],
    ['fails', (request) => request.reject(new AxiosError('Network Error', AxiosError.ERR_NETWORK))],
  ])('ignores a replaced request that %s after the current one succeeds', async (_, settleLate) => {
    const late = {}
    vi.mocked(getTracking).mockImplementationOnce(
      () => new Promise((resolve, reject) => Object.assign(late, { resolve, reject })),
    )
    const { result, rerender } = renderTracking('WB10000001')
    rerender({ waybill: 'WB10000002' })
    await finishRequests()

    await act(async () => settleLate(late))

    expect(result.current.state).toBe('success')
    expect(result.current.data.waybill).toBe('WB10000002')
  })

  it('aborts the request on unmount', () => {
    const { unmount } = renderTracking('WB10000001')

    unmount()

    const signal = vi.mocked(getTracking).mock.calls[0][1]
    expect(signal.aborted).toBe(true)
  })

  it('fetches again on retry', async () => {
    const { result } = renderTracking('WB99999999')
    await finishRequests()

    act(() => result.current.retry())

    expect(result.current.state).toBe('loading')
    await finishRequests()
    expect(result.current.state).toBe('notFound')
    expect(getTracking).toHaveBeenCalledTimes(2)
  })
})
