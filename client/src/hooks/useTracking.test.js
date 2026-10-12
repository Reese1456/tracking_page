import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { getTracking } from '../api/trackingClient.js'
import { useTracking } from './useTracking.js'

// The real mock client still runs; spying only records each call's signal.
vi.mock('../api/trackingClient.js', { spy: true })

function renderTracking(waybill) {
  return renderHook((props) => useTracking(props.waybill), { initialProps: { waybill } })
}

describe('useTracking', () => {
  it('is idle when there is no waybill', () => {
    const { result } = renderTracking('')

    expect(result.current.state).toBe('idle')
    expect(getTracking).not.toHaveBeenCalled()
  })

  it('reaches success with the shipment for a known waybill', async () => {
    const { result } = renderTracking('WB10000004')

    expect(result.current.state).toBe('loading')
    await waitFor(() => expect(result.current.state).toBe('success'))
    expect(result.current.data.waybill).toBe('WB10000004')
    expect(result.current.data.stepIndex).toBe(3)
  })

  it('reaches notFound for an unknown waybill', async () => {
    const { result } = renderTracking('WB99999999')

    await waitFor(() => expect(result.current.state).toBe('notFound'))
    expect(result.current.data).toBeNull()
    expect(result.current.error.response.status).toBe(404)
  })

  it('reaches error for a badly formatted waybill', async () => {
    const { result } = renderTracking('hello')

    await waitFor(() => expect(result.current.state).toBe('error'))
    expect(result.current.error.response.data.title).toBe('Invalid waybill format')
  })

  it('shows loading, not the previous shipment, as soon as the waybill changes', async () => {
    const { result, rerender } = renderTracking('WB10000001')
    await waitFor(() => expect(result.current.state).toBe('success'))

    rerender({ waybill: 'WB10000002' })

    expect(result.current.state).toBe('loading')
    expect(result.current.data).toBeNull()
    await waitFor(() => expect(result.current.data?.waybill).toBe('WB10000002'))
  })

  it('aborts the previous request when the waybill changes mid-request', async () => {
    const { result, rerender } = renderTracking('WB10000001')

    rerender({ waybill: 'WB10000002' })

    const firstSignal = vi.mocked(getTracking).mock.calls[0][1]
    expect(firstSignal.aborted).toBe(true)
    await waitFor(() => expect(result.current.state).toBe('success'))
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
    await waitFor(() => expect(result.current.state).toBe('notFound'))

    act(() => result.current.retry())

    expect(result.current.state).toBe('loading')
    await waitFor(() => expect(result.current.state).toBe('notFound'))
    expect(getTracking).toHaveBeenCalledTimes(2)
  })
})
