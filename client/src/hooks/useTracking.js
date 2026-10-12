import { useCallback, useEffect, useState } from 'react'
import { getTracking } from '../api/trackingClient.js'

/**
 * Fetches tracking for a waybill. `state` is 'idle' | 'loading' | 'success' |
 * 'notFound' | 'error'. 'loading' is never stored: it is any render whose
 * request has no result yet.
 */
export function useTracking(waybill) {
  // Every request gets a new id, including a return to an earlier waybill,
  // so a result can only ever match the request that produced it.
  const [request, setRequest] = useState({ waybill, id: 0 })
  const [result, setResult] = useState(null)

  // Updating state during render, rather than in an effect, is React's way to
  // respond to a changed prop: React re-renders straight away, before
  // anything reaches the screen, so the old result is never shown under the
  // new waybill.
  if (request.waybill !== waybill) {
    setRequest({ waybill, id: request.id + 1 })
  }

  useEffect(() => {
    if (!request.waybill) {
      return
    }

    const controller = new AbortController()

    // Once aborted, this request has been replaced or its component is gone.
    // Checking here, not just relying on the abort, also covers an answer
    // that had already arrived before the abort happened.
    getTracking(request.waybill, controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) {
          setResult({ id: request.id, state: 'success', data, error: null })
        }
      })
      .catch((error) => {
        if (!controller.signal.aborted) {
          const state = error.response?.status === 404 ? 'notFound' : 'error'
          setResult({ id: request.id, state, data: null, error })
        }
      })

    return () => controller.abort()
  }, [request])

  const retry = useCallback(() => setRequest((current) => ({ ...current, id: current.id + 1 })), [])

  if (!waybill) {
    return { state: 'idle', data: null, error: null, retry }
  }
  if (result?.id !== request.id) {
    return { state: 'loading', data: null, error: null, retry }
  }
  return { state: result.state, data: result.data, error: result.error, retry }
}
