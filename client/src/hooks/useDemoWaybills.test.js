import { act, renderHook } from '@testing-library/react'
import { AxiosError } from 'axios'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getDemoWaybills } from '../api/trackingClient.js'
import { useDemoWaybills } from './useDemoWaybills.js'

vi.mock('../api/trackingClient.js', { spy: true })

function finishRequests() {
  return act(() => vi.advanceTimersByTimeAsync(400))
}

describe('useDemoWaybills', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('starts empty, then returns the demo waybills', async () => {
    const { result } = renderHook(() => useDemoWaybills())
    expect(result.current).toEqual([])

    await finishRequests()

    expect(result.current).toEqual([
      'WB10000001',
      'WB10000002',
      'WB10000003',
      'WB10000004',
      'WB10000005',
      'WB10000006',
    ])
  })

  it('aborts the request on unmount', () => {
    const { unmount } = renderHook(() => useDemoWaybills())

    unmount()

    const signal = vi.mocked(getDemoWaybills).mock.calls[0][0]
    expect(signal.aborted).toBe(true)
  })

  // Vitest fails the run on an unhandled rejection, so this also proves the
  // failure is caught.
  it('stays empty when the request fails', async () => {
    vi.mocked(getDemoWaybills).mockRejectedValueOnce(new AxiosError('Network Error', AxiosError.ERR_NETWORK))
    const { result } = renderHook(() => useDemoWaybills())

    await finishRequests()

    expect(result.current).toEqual([])
  })
})
