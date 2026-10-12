import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { getTracking } from '../api/trackingClient.js'

/**
 * Fetches tracking for a waybill. `state` is 'idle' | 'loading' | 'success' |
 * 'notFound' | 'error'. 'loading' is never stored: it is any render whose
 * request has no result yet.
 */
export function useTracking(waybill) {
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState(null)

  // Each result is tagged with the request it answers. When the waybill
  // changes or retry is called, the key changes and the old result stops
  // counting straight away, so the previous shipment is never shown under the
  // new waybill, even for one render.
  const requestKey = `${waybill}#${attempt}`

  useEffect(() => {
    if (!waybill) {
      return
    }

    const controller = new AbortController()

    getTracking(waybill, controller.signal)
      .then((data) => setResult({ key: requestKey, state: 'success', data, error: null }))
      .catch((error) => {
        if (axios.isCancel(error)) {
          return
        }
        const state = error.response?.status === 404 ? 'notFound' : 'error'
        setResult({ key: requestKey, state, data: null, error })
      })

    return () => controller.abort()
  }, [waybill, requestKey])

  const retry = useCallback(() => setAttempt((n) => n + 1), [])

  if (!waybill) {
    return { state: 'idle', data: null, error: null, retry }
  }
  if (result?.key !== requestKey) {
    return { state: 'loading', data: null, error: null, retry }
  }
  return { state: result.state, data: result.data, error: result.error, retry }
}
