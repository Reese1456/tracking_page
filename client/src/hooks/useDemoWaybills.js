import { useEffect, useState } from 'react'
import { getDemoWaybills } from '../api/trackingClient.js'

export function useDemoWaybills() {
  const [waybills, setWaybills] = useState([])

  useEffect(() => {
    const controller = new AbortController()

    getDemoWaybills(controller.signal)
      .then((list) => {
        if (!controller.signal.aborted) {
          setWaybills(list)
        }
      })
      // The chips are only a shortcut for trying the demo. Without them the
      // search box still works, so a failure isn't worth an error message.
      .catch(() => {})

    return () => controller.abort()
  }, [])

  return waybills
}
